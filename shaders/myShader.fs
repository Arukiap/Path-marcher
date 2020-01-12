#version 410 core

in vec4 gl_FragCoord;

varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying mat3 v_cameraMatrix;
varying float v_fov;
varying float v_time;

//Math constants
const float PI = 3.1415926;

//Ray Marching constants
const int MAX_MARCHING_STEPS = 128;
const float MIN_DIST = 0.0;
const float MAX_DIST = 100.0;
const float EPSILON = 0.001;

//Path marching constants and globals
const int MAX_MARCH_DEPTH = 4;
vec3 samplePixelColor = vec3(0.0,0.0,0.0);
vec3 pixelColor = vec3(0.0,0.0,0.0);
const float refractionIndex = 1.517;

//Constants for multi sampling
const int NUM_OF_SAMPLES = 1;


//Scene constants
const int sceneObjectNumber = 1;
const vec3 sceneBackgroundColor = vec3(0.5,0.8,1.0);
//const vec3 sceneBackgroundColor = vec3(1.0);

//Represents an object
struct Object {
	vec3 center;
	float size;
	int type; //0 for sphere, 1 for cube, 2 for plane
	vec3 albedo; //color of the object 
	double emission; //if non 0 it is a light source
	int surfaceType; //diffuse 0, specular 1 or refractive 2 
	int id; //object id
};

//Represents the current scene
struct Scene {
	Object sceneObjects[sceneObjectNumber];
};

//Scene instantiation
Scene globalScene = Scene(Object[sceneObjectNumber](
	//Object(vec3(-0.1,0.0,0.0),0.2,1,vec3(1.0,0.0,0.0),0.0,0,0), //cube
	//Object(vec3(-0.5,0.0,0.0),3,3,vec3(1.0,1.0,1.0),0.0,0,1), //left wall
	//Object(vec3(0.5,0.0,0.0),2,3,vec3(1.0,1.0,1.0),0.0,0,2), //right wall
	//Object(vec3(0.0,0.5,-1.5),0.5,2,vec3(1.0,1.0,1.0),0.0,0,3), //ceiling 1
	//Object(vec3(0.0,0.5,1.5),0.5,2,vec3(1.0,1.0,1.0),0.0,0,3), //ceiling 2
	//Object(vec3(0.0,0.5,0.0),0.5,2,vec3(1.0,1.0,1.0),0.0,0,3), //ceiling 3
	Object(vec3(0.0,0.0,0.0),0.5,4,vec3(1.0,1.0,1.0),0.0,0,3) //julia fractal
	//Object(vec3(0.0,-0.3,0.0),20.0,2,vec3(1.0,0.5,0.0),0.0,0,2) //plane
));

vec3 lightSource = vec3(4.0,10.0,10.0);

struct SceneCollision {
	float distance;
	vec3 color;
	int objectId;
};

//Simple shapes

float sphereDistance(vec3 currentPoint, vec3 center, float radius){
	return length(currentPoint-center) - radius;
}

float cubeDistance(vec3 currentPoint, vec3 center, float sideLength){
	vec3 q = abs(currentPoint-center) - vec3(sideLength);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float wallDistance(vec3 currentPoint, vec3 center, float sideLength){
	vec3 q = abs(currentPoint-center) - vec3(0.1,2.5,sideLength);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float planeDistance(vec3 currentPoint, vec3 center, float size){
	vec3 q = abs(currentPoint-center) - vec3(size,0.1,size);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float torusDistance(vec3 currentPoint, vec2 size){
	vec2 q = vec2(length(currentPoint.xz)-size.x,currentPoint.y);
  	return length(q)-size.y;
}

//Fractals
float juliaFractalDistance(vec3 currentPoint) {
	const float BAILOUT = 10.0;
	vec4 p = vec4(currentPoint, 0.0);
	vec4 dp = vec4(1.0,0.0,0.0,0.0);
	for (int i = 0; i < 10; i++) {
		dp = 2.0* vec4(p.x*dp.x-dot(p.yzw, dp.yzw), p.x*dp.yzw+dp.x*p.yzw+cross(p.yzw, dp.yzw));
		p = vec4(p.x*p.x-dot(p.yzw, p.yzw), vec3(2.0*p.x*p.yzw))+0.37;
		float p2 = dot(p,p);
		if (p2 > BAILOUT) break;
	}
	float r = length(p);
	return  0.5 * r * log(r) / length(dp);
}

SceneCollision getObjectDistanceAsCollision(vec3 ray, Object object){
	switch (object.type) {
		case 0: //sphere
				return SceneCollision(sphereDistance(ray,object.center,object.size/2),object.albedo,object.id);
		case 1: //cube
				return SceneCollision(cubeDistance(ray,object.center,object.size),object.albedo,object.id);
		case 2: //plane
				return SceneCollision(planeDistance(ray,object.center,object.size),object.albedo,object.id);
		case 3: //wall
				return SceneCollision(wallDistance(ray,object.center,object.size),object.albedo,object.id);
		case 4: //fractal
				return SceneCollision(juliaFractalDistance(ray),object.albedo,object.id);
	}
	return SceneCollision(MAX_DIST,sceneBackgroundColor,-1);
}

SceneCollision getClosestSceneObjectAsCollision(vec3 ray){

	SceneCollision minimumCollision = SceneCollision(MAX_DIST,sceneBackgroundColor,-1);

	for(int i = 0; i < globalScene.sceneObjects.length(); i++){
		SceneCollision currentCollision = getObjectDistanceAsCollision(ray,globalScene.sceneObjects[i]);
		if(minimumCollision.distance > currentCollision.distance){
			minimumCollision = currentCollision;
		}
	}
	return minimumCollision;
}

/*
 * Ray marching algorithm.
 * Returns aprox. distance to the scene from a certain point with a certain direction.
 */
SceneCollision rayMarchScene(vec3 from, vec3 direction) {
	float totalDistance = 0.0;
	SceneCollision sceneCollision;
	for (int steps = 0; steps < MAX_MARCHING_STEPS; steps++){
		vec3 ray = from + totalDistance * direction;
		sceneCollision = getClosestSceneObjectAsCollision(ray);
		totalDistance += sceneCollision.distance;
		if(sceneCollision.distance > MAX_DIST || sceneCollision.distance < EPSILON) break;
	}
	return SceneCollision(totalDistance,sceneCollision.color,sceneCollision.objectId);
}

/*
 * Utilizes a vec2 seed in order to produce a random floating point value
 */
float random(vec2 seed) {
	return fract(sin(dot(seed.xy,vec2(20.234234,23490.234234)))*4002.12);
    //return fract(sin(dot(seed.xy,vec2(20.234234,23490.234234)))*v_time);
}

mat3 getTangentSpace(vec3 normal){

	vec3 h = vec3(1.0,0.0,0.0);
	if(abs(normal.x)>0.99){
		h = vec3(0.0,0.0,1.0);
	}

	vec3 tangent = normalize(cross(normal,h));
	vec3 binormal = normalize(cross(normal,tangent));
	return mat3(tangent,binormal,normal);
}

/*
 * Utilizes two random floating point values to produce a random sample of a three-dimensional
 * vector on the normal hemisphere.
 */
vec3 sampleHemisphere(float u1, float u2, vec3 normal){
	//float r = sqrt(1.0-u1*u1);
	//float phi = 2*PI*u2;
	//vec3 tangentSpaceDirection = vec3(cos(phi)*r,sin(phi)*r,u1);
	//return tangentSpaceDirection*getTangentSpace(normal);
	vec3 uu = normalize(cross(normal,vec3(0.0,1.0,1.0)));
	vec3 vv = normalize(cross(uu,normal));

	float ra = sqrt(u1);
	float rx = ra*cos(10.2831*u2);
	float ry = ra*sin(10.2831*u2);
	float rz = sqrt(1.0-u1);
	vec3 rr = vec3(rx*uu + ry*vv + rz*normal);

	return normalize(rr);
}

/*
 * Returns an aprox. normal vector a given surface point.
 */
vec3 getNormal(vec3 surfacePoint){
	float distanceToPoint = getClosestSceneObjectAsCollision(surfacePoint).distance;
	vec2 e = vec2(.001,0); //epsilon vector
	vec3 normal = vec3(
        getClosestSceneObjectAsCollision(surfacePoint+e.xyy).distance - getClosestSceneObjectAsCollision(surfacePoint-e.xyy).distance,
        getClosestSceneObjectAsCollision(surfacePoint+e.yxy).distance - getClosestSceneObjectAsCollision(surfacePoint-e.yxy).distance,
        getClosestSceneObjectAsCollision(surfacePoint+e.yyx).distance - getClosestSceneObjectAsCollision(surfacePoint-e.yyx).distance
    );
	return normalize(normal);
}
















































/*
 * "Path marching" algorithm.
 */
void march(vec3 from, vec3 direction, int depth, int sampleNumber) {

	while(depth <= MAX_MARCH_DEPTH){

	SceneCollision intersectionWithScene = rayMarchScene(from,direction);

	if(intersectionWithScene.objectId == -1){
		samplePixelColor+= sceneBackgroundColor*0.5/depth;
		return;
	};

	Object intersectedObject = globalScene.sceneObjects[intersectionWithScene.objectId];

	vec3 hitpoint = from + intersectionWithScene.distance * direction;

	vec3 normal = getNormal(hitpoint);

	//samplePixelColor += vec3(intersectedObject.emission)*2/depth;
	//samplePixelColor += directionToLightSource*2/depth;

	if(intersectedObject.surfaceType == 0){

		//next event estimation
		vec3 directionToLightSource = normalize(lightSource-hitpoint);
		SceneCollision intersectionWithLight = rayMarchScene(hitpoint + normal * 4 * EPSILON,directionToLightSource);
		bool isOccluded = intersectionWithLight.distance < length(directionToLightSource);
		if(!isOccluded){
			samplePixelColor += vec3(clamp(dot(normal,directionToLightSource),0.0,0.5))*(intersectionWithLight.distance*0.00012)/depth;
			//samplePixelColor += (intersectionWithLight.distance*0.00006)/depth;
		}

		float sample1 = random( gl_FragCoord.yx*sampleNumber/v_resolution.xy );
		float sample2 = random( v_resolution.xy*sampleNumber/gl_FragCoord.xy );
		vec3 newRayDirection = sampleHemisphere(sample1,sample2,normal);
		float cost = dot(newRayDirection,normal);
		//samplePixelColor += cost*(intersectedObject.albedo)*0.2/depth;
		samplePixelColor = mix(samplePixelColor,intersectedObject.albedo,cost*0.2/depth);
		from = hitpoint + normal * EPSILON * 4;
		direction = newRayDirection;

	} else if(intersectedObject.surfaceType == 1){

		vec3 newRayDirection = reflect(direction,normal);
		from = hitpoint + normal * EPSILON * 4;
		direction = newRayDirection;

	} else if(intersectedObject.surfaceType == 2){

		float rIndex = refractionIndex;

		//Probability of reflection in normal incidence
		float reflectionProbability = (1.0-rIndex)/(1.0+rIndex);

		//3D Snell's law
		if(dot(normal,direction) > 0){
			normal = normal * -1;
			rIndex = 1/rIndex;
		}

		rIndex = 1/rIndex;
		float cosin1 = dot(normal,direction)*-1;
		float cosin2 = 1.0-pow(rIndex,2)*(1.0-pow(cosin1,2));

		if(cosin2 > 0){
			direction = normalize(direction*rIndex + (normal*(rIndex*cosin1-sqrt(cosin2))));
		}

		from = hitpoint + normal * EPSILON * 4;
	}

	
	
	depth += 1;
	}
}
























vec3 rayDirection(float fov, vec2 size, vec2 fragCoord, mat3 cameraMatrix){
    vec2 xy = fragCoord - size / 2.0;
    float z = size.y / tan(radians(fov)/2.0);
    return ( cameraMatrix * normalize(vec3(xy,z)));
}

void main(){	

    vec3 direction = rayDirection(v_fov,v_resolution,gl_FragCoord.xy, v_cameraMatrix);
    vec3 eye = v_cameraPosition;

	for (int sampleNumber = 1; sampleNumber < NUM_OF_SAMPLES+1; sampleNumber++){
		march(eye,direction,1,sampleNumber);
		pixelColor += samplePixelColor;
		samplePixelColor = vec3(0.0,0.0,0.0);
	}

	pixelColor = pixelColor/NUM_OF_SAMPLES;

	gl_FragColor = vec4(pixelColor,1.0);
		//float rnd = random( gl_FragCoord.xy/v_resolution.xy );

		//vec3 hemisphereSample = sampleHemisphere(rnd,fract(rnd/10));

    //gl_FragColor = vec4(vec3(marchedDistance/20),1.0);
		//SceneCollision sceneCollision = rayMarchScene(eye,direction);
		
		//if(sceneCollision.objectId != -1){
		//	gl_FragColor = vec4(sceneCollision.color/sceneCollision.distance,1.0);
		//} else { 
		//	gl_FragColor = vec4(sceneBackgroundColor,1.0);
		//}
		
} 