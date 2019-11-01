#version 410 core

in vec4 gl_FragCoord;

varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying mat3 v_cameraMatrix;
varying float v_fov;

//Math constants
const float PI = 3.1415926;

//Ray Marching constants
const int MAX_MARCHING_STEPS = 128;
const float MIN_DIST = 0.0;
const float MAX_DIST = 100.0;
const float EPSILON = 0.01;

//Path marching constants and globals
const int MAX_MARCH_DEPTH = 5;
vec3 pixelColor = vec3(0.0,0.0,0.0);

//Scene constants
const int sceneObjectNumber = 4;
const vec3 sceneBackgroundColor = vec3(0.2,0.6,1.0);


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
const Scene globalScene = Scene(Object[sceneObjectNumber](
	Object(vec3(0.5,-0.1,0.0),0.2,0,vec3(1.0,1.0,0.0),0.0,0,0),
	Object(vec3(1.0,0.0,0.0),0.4,0,vec3(1.0,0.2,0.6),1.0,0,	1),
	Object(vec3(2.0,0.0,0.0),0.2,1,vec3(0.3,0.4,0.8),0.0,0,2),
	Object(vec3(0.0,-0.3,0.0),10.0,2,vec3(1.0,1.0,1.0),0.0,0,3)
));

struct SceneCollision {
	float distance;
	vec3 color;
	int objectId;
};

float sphereDistance(vec3 currentPoint, vec3 center, float radius){
	return length(currentPoint-center) - radius;
}

float cubeDistance(vec3 currentPoint, vec3 center, float sideLength){
	vec3 q = abs(currentPoint-center) - vec3(sideLength);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float planeDistance(vec3 currentPoint, vec3 center, float size){
	vec3 q = abs(currentPoint-center) - vec3(size,0.1,size);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

SceneCollision getObjectDistanceAsCollision(vec3 ray, Object object){
	switch (object.type) {
		case 0: //sphere
				return SceneCollision(sphereDistance(ray,object.center,object.size/2),object.albedo,object.id);
		case 1: //cube
				return SceneCollision(cubeDistance(ray,object.center,object.size),object.albedo,object.id);
		case 2: //plane
				return SceneCollision(planeDistance(ray,object.center,object.size),object.albedo,object.id);
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
    return fract(sin(dot(seed.xy,vec2(20.234234,23490.234234)))*43758.985);
}

/*
 * Utilizes two random floating point values to produce a random sample of a three-dimensional
 * vector on the normal hemisphere.
 */
vec3 sampleHemisphere(float u1, float u2){
	float r = sqrt(1.0-u1*u1);
	float phi = 2*PI*u2;
	return vec3(cos(phi)*r,sin(phi)*r,u1);
}

/*
 * Returns an aprox. normal vector a given surface point.
 */
vec3 getNormal(vec3 surfacePoint){
	float distanceToPoint = getClosestSceneObjectAsCollision(surfacePoint).distance;
	vec2 e = vec2(.01,0); //epsilon vector
	vec3 normal = distanceToPoint - vec3(
        getClosestSceneObjectAsCollision(surfacePoint-e.xyy).distance,
        getClosestSceneObjectAsCollision(surfacePoint-e.yxy).distance,
        getClosestSceneObjectAsCollision(surfacePoint-e.yyx).distance
    );
	return normalize(normal);
}
















































/*
 * "Path marching" algorithm.
 */
void march(vec3 from, vec3 direction, int depth) {

	while(depth <= MAX_MARCH_DEPTH){

	SceneCollision intersectionWithScene = rayMarchScene(from,direction);

	if(intersectionWithScene.objectId == -1) return;

	Object intersectedObject = globalScene.sceneObjects[intersectionWithScene.objectId];

	vec3 hitpoint = from + intersectionWithScene.distance * direction;

	pixelColor += vec3(intersectedObject.emission);

	vec3 normal = getNormal(hitpoint);

	if(intersectedObject.surfaceType == 0){
		float sample1 = random( gl_FragCoord.xy/v_resolution.xy );
		float sample2 = random( v_resolution.xy/gl_FragCoord.xy );
		vec3 newRayDirection = normal + sampleHemisphere(sample1,sample2);
		float cost = dot(newRayDirection,normal);
		pixelColor += cost*(intersectedObject.albedo)*0.1;
		from = hitpoint;
		direction = newRayDirection;
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

    march(eye,direction,0);

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