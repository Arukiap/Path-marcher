#version 410 core

in vec4 gl_FragCoord;

//layout(location = 0) out vec3 color;

layout (location = 2) in vec2 texCoords;

uniform sampler2D blueNoise;
uniform sampler2D inputTexture;

varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying mat3 v_cameraMatrix;
varying float v_fov;
varying float v_time;
varying float v_hasCameraChanged;

//Math constants
const float PI = 3.1415926;

//Ray Marching constants
const int MAX_MARCHING_STEPS = 128;
const float MIN_DIST = 0.0;
const float MAX_DIST = 100.0;
const float EPSILON = 0.001;

//Path marching constants and globals
const int MAX_MARCH_DEPTH = 4; // bounces
vec3 samplePixelColor = vec3(0.0,0.0,0.0);
vec3 pixelColor = vec3(0.0,0.0,0.0);
float distanceToScene = MAX_DIST;
const float refractionIndex = 1.517;

//Constants for multi sampling
const int NUM_OF_SAMPLES = 1;


//Scene constants
const int sceneObjectNumber = 3;
const vec3 sceneBackgroundColor = vec3(0.5,0.8,1.0);
//const vec3 sceneBackgroundColor = vec3(1.0,0.647,0.0);
//const vec3 sceneBackgroundColor = vec3(0.0,0.0,0.0);
const vec3 sceneFogColor = vec3(1.0,1.0,1.0);
const bool isFractalMode = false;
//const vec3 sceneBackgroundColor = vec3(1.0);

//Represents an object
struct Object {
	vec3 center;
	float size;
	int type; //0 for sphere, 1 for cube, 2 for plane, 3 for torus, 4 for prism, 5 for pyramid, 6 for mandelbulb, 7 for wall
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
	Object(vec3(2.4,0.35,0.0),0.7,0,vec3(1.0,0.0,0.0),2.0,0,0), //sphere
	Object(vec3(0.0,0.25,0.0),0.5,3,vec3(0.0,0.0,1.0),0.0,0,1), //cube
	//Object(vec3(-1.0,0.25,2.0),0.5,3,vec3(1.0,1.0,1.0),0.0,0,2), //torus
	//Object(vec3(-2.0,0.25,3.0),0.5,4,vec3(1.0,1.0,1.0),0.0,0,3), //prism
	//Object(vec3(1.6,0.25,0.0),0.5,0,vec3(0.0,1.0,0.0),0.0,1,2),
	//Object(vec3(0.0,0.25,0.0),0.5,1,vec3(1.0,0.0,0.0),0.0,0,1), //cube 2
	//Object(vec3(0.60,0.25,0.25),0.25,1,vec3(0.0,0.0,1.0),0.0,0,1), //cube 3
	//Object(vec3(1.0,0.0,0.0),0.25,1,vec3(0.3,0.3,1.0),0.0,1,1), //cube 3
	//Object(vec3(.5,0.0,0.0),2,3,vec3(1.0,1.0,1.0),0.0,0,2), //left wall
	//Object(vec3(-0.5,0.0,0.0),2,3,vec3(1.0,1.0,1.0),0.0,0,2), //right wall
	//Object(vec3(0.0,0.5,-1.5),0.5,2,vec3(1.0,1.0,1.0),0.0,0,3), //ceiling 1
	//Object(vec3(0.0,0.5,1.5),0.5,2,vec3(1.0,1.0,1.0),0.0,0,4), //ceiling 2
	//Object(vec3(0.0,0.5,0.0),0.5,2,vec3(1.0,1.0,1.0),0.0,0,5), //ceiling 3
	//Object(vec3(0.0,0.0,0.0),1.0,4,vec3(0.0,0.0,0.0),0.0,0,0) //julia fractal
	//Object(vec3(2.0,0.5,0.0),1.0,7,vec3(1.0,1.0,1.0),0.0,0,0),
	Object(vec3(0.0,0,0.0),100.0,2,vec3(1.0,1.0,1.0),0.0,0,2) //plane
));

//vec3 lightSource = vec3(3.0,4.0,-4.4);
//vec3 lightSource = vec3(100.0,47.0,95.4);
vec3 lightSource = vec3(2.0,1.39,3.0);

struct SceneCollision {
	float distance;
	vec3 color;
	int objectId;
};

//SDF operations


float opUnion( float d1, float d2 ) {  return min(d1,d2); }

float opSubtraction( float d1, float d2 ) { return max(-d1,d2); }

float opIntersection( float d1, float d2 ) { return max(d1,d2); }

//Simple shapes

float sphereDistance(vec3 currentPoint, vec3 center, float radius){
	return length(currentPoint-center) - radius;
}

float cubeDistance(vec3 currentPoint, vec3 center, float sideLength){
	vec3 q = abs(currentPoint-center) - vec3(sideLength);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float wallDistance(vec3 currentPoint, vec3 center, float sideLength){
	vec3 q = abs(currentPoint-center) - vec3(0.9,0.9,sideLength);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float planeDistance(vec3 currentPoint, vec3 center, float size){
	vec3 q = abs(currentPoint-center) - vec3(size,0.01,size);
	return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float torusDistance(vec3 currentPoint, vec3 center, vec2 size){
	currentPoint = currentPoint - center;
	vec2 q = vec2(length(currentPoint.xz)-size.x,currentPoint.y);
  	return length(q)-size.y/2;
}

float prismDistance( vec3 currentPoint, vec3 center, vec2 size){
	currentPoint = currentPoint - center;
  const float k = sqrt(3.0);
  size.x *= 0.5*k;
  currentPoint.xy /= size.x;
  currentPoint.x = abs(currentPoint.x) - 1.0;
  currentPoint.y = currentPoint.y + 1.0/k;
  if( currentPoint.x+k*currentPoint.y>0.0 ) currentPoint.xy=vec2(currentPoint.x-k*currentPoint.y,-k*currentPoint.x-currentPoint.y)/2.0;
  currentPoint.x -= clamp( currentPoint.x, -2.0, 0.0 );
  float d1 = length(currentPoint.xy)*sign(-currentPoint.y)*size.x;
  float d2 = abs(currentPoint.z)-size.y;
  return length(max(vec2(d1,d2),0.0)) + min(max(d1,d2), 0.);
}

float pyramidDistance( vec3 currentPoint, vec3 center, float size){
	currentPoint = currentPoint - center;
  float m2 = size*size + 0.25;
    
  currentPoint.xz = abs(currentPoint.xz);
  currentPoint.xz = (currentPoint.z>currentPoint.x) ? currentPoint.zx : currentPoint.xz;
  currentPoint.xz -= 0.5;

  vec3 q = vec3( currentPoint.z, size*currentPoint.y - 0.5*currentPoint.x, size*currentPoint.x + 0.5*currentPoint.y);
   
  float s = max(-q.x,0.0);
  float t = clamp( (q.y-0.5*currentPoint.z)/(m2+0.25), 0.0, 1.0 );
    
  float a = m2*(q.x+s)*(q.x+s) + q.y*q.y;
  float b = m2*(q.x+0.5*t)*(q.x+0.5*t) + (q.y-m2*t)*(q.y-m2*t);
    
  float d2 = min(q.y,-q.x*m2-q.y*0.5) > 0.0 ? 0.0 : min(a,b);
    
  return sqrt( (d2+q.z*q.z)/m2 ) * sign(max(q.z,-currentPoint.y));
}


//Fractals
float juliaFractalDistance(vec3 currentPoint) {
	const float BAILOUT = 10.0;
	vec4 p = vec4(currentPoint, 0.0);
	vec4 dp = vec4(1.0,0.0,0.0,0.0);
	for (int i = 0; i < 10; i++) {
		dp = 2.0* vec4(p.x*dp.x-dot(p.yzw, dp.yzw), p.x*dp.yzw+dp.x*p.yzw+cross(p.yzw, dp.yzw));
		p = vec4(p.x*p.x-dot(p.yzw, p.yzw), vec3(2.0*p.x*p.yzw))+0.38;
		float p2 = dot(p,p);
		if (p2 > BAILOUT) break;
	}
	float r = length(p);
	return  0.5 * r * log(r) / length(dp);
}

float mandelboxFractalDistance(vec3 currentPoint) {
  float SCALE = 2.8;
  float MR2 = 0.2;
  int ITERATIONS = 10;

  vec4 scalevec = vec4(SCALE, SCALE, SCALE, abs(SCALE)) / MR2;
  float C1 = abs(SCALE-1.0), C2 = pow(abs(SCALE), float(1-ITERATIONS));

  // distance estimate
  vec4 p = vec4(currentPoint.xyz, 1.0), p0 = vec4(currentPoint.xyz, 1.0);  // p.w is knighty's DEfactor
  
  for (int i=0; i<ITERATIONS; i++) {
    p.xyz = clamp(p.xyz, -1.0, 1.0) * 2.0 - p.xyz;  // box fold: min3, max3, mad3
    float r2 = dot(p.xyz, p.xyz);  // dp3
    p.xyzw *= clamp(max(MR2/r2, MR2), 0.0, 1.0);  // sphere fold: div1, max1.sat, mul4
    p.xyzw = p*scalevec + p0;  // mad4
  }
  return ((length(p.xyz) - C1) / p.w) - C2;
}

float mandelbulbFractalDistance(vec3 currentPoint, vec3 center, float size) {
	currentPoint = currentPoint - center;
	int ITERATIONS = 10;
	float BAILOUT = 10.0;
	int POWER = 9;

	vec3 z = currentPoint;
	float dr = 1.0;
	float r = 0.0;
	for (int i = 0; i < ITERATIONS ; i++) {
		r = length(z);

		if (r>BAILOUT) break;

		// convert to polar coordinates
		float theta = acos(z.z/r);
		float phi = atan(z.y,z.x);
		dr =  pow( r, POWER-1.0 )*POWER*dr + 1.0;
		
		// scale and rotate the point
		float zr = pow( r,POWER);
		theta = theta*POWER;
		phi = phi*POWER;
		
		// convert back to cartesian coordinates
		z = zr*vec3(sin(theta)*cos(phi), sin(phi)*sin(theta), cos(theta));

		z+=currentPoint;
	}
	return 0.5*log(r)*r/dr;
}




























//0 for sphere, 1 for cube, 2 for plane, 3 for torus, 4 for prism, 5 for pyramid, 6 for mandelbulb

SceneCollision getObjectDistanceAsCollision(vec3 ray, Object object){
	switch (object.type) {
		case 0: //sphere
				return SceneCollision(sphereDistance(ray,object.center,object.size/2),object.albedo,object.id);
		case 1: //cube
				//return SceneCollision(opSubtraction(wallDistance(ray,object.center+vec3(0.0,0.0,0.1),object.size*1.9),cubeDistance(ray,object.center,object.size*2)),object.albedo,object.id);
				return SceneCollision(cubeDistance(ray,object.center,object.size),object.albedo,object.id);
		case 2: //plane
				return SceneCollision(planeDistance(ray,object.center,object.size),object.albedo,object.id);
		case 3: //torus
				return SceneCollision(torusDistance(ray,object.center,vec2(object.size)),object.albedo,object.id);
		case 4: //prism
				return SceneCollision(prismDistance(ray,object.center,vec2(object.size)),object.albedo,object.id);
		case 5: //pyramid
				return SceneCollision(pyramidDistance(ray,object.center,object.size),object.albedo,object.id);
		case 6: //mandelbulb
				return SceneCollision(mandelbulbFractalDistance(ray,object.center,object.size),object.albedo,object.id);
		case 7: //wall
				return SceneCollision(wallDistance(ray,object.center,object.size),object.albedo,object.id);
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

	if(depth == 1){
		distanceToScene = intersectionWithScene.distance;
	}

	vec3 normal = getNormal(hitpoint);

	//samplePixelColor += vec3(intersectedObject.emission)*2/depth;
	//samplePixelColor += directionToLightSource*2/depth;

	if(intersectedObject.surfaceType == 0){

		//next event estimation
		vec3 directionToLightSource = normalize(lightSource-hitpoint);
		SceneCollision intersectionWithLight = rayMarchScene(hitpoint + normal * 4 * EPSILON,directionToLightSource);
		bool isOccluded = intersectionWithLight.distance < length(directionToLightSource);
		if(!isOccluded){
			samplePixelColor += vec3(clamp(dot(normal,directionToLightSource),0.0,0.5))*(intersectionWithLight.distance*0.0001)/depth;
			//samplePixelColor += (intersectionWithLight.distance*0.00006)/depth;
		}

		float sample1 = random( gl_FragCoord.yx*sampleNumber/v_resolution.xy*v_time/10000 );
		float sample2 = random( v_resolution.xy*sampleNumber/gl_FragCoord.xy*v_time/10000 );
		//Now with blue noise:
		//vec4 blueNoiseSample = texture2D(blueNoise,vec2((gl_FragCoord.xy/v_resolution.xy)*sampleNumber));
		//float sample1 = blueNoiseSample.x;
		//float sample2 = blueNoiseSample.y;
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
//+texture2D(blueNoise,gl_FragCoord.xy).xy*0.05
	
	vec3 finalColor;

	if(v_hasCameraChanged > 0.5){
		//vec3 mixedFogColor = mix(pixelColor,sceneBackgroundColor,distanceToScene*10/MAX_DIST);
		//gl_FragColor = vec4(pixel,1.0);
		finalColor = pixelColor;
	} else {
		vec4 previousPixel = texture(inputTexture,texCoords.xy); //+ vec4(pixelColor,1.0);
		//vec3 mixedColor = mix(previousPixel.xyz,pixelColor,0.05);
		vec3 mixedColor = mix(previousPixel.xyz,pixelColor,0.05);
		//vec3 mixedFogColor = mix(pixelColor,sceneBackgroundColor,distanceToScene*10/MAX_DIST);
		finalColor = mixedColor;
	}

	//vec3 foggyColor = mix(finalColor,sceneFogColor,distanceToScene/MAX_DIST);
	gl_FragColor = vec4(finalColor,1.0);
	
	
	//color = vec4(pixelColor+previousPixel.xyz,1.0);
	//gl_FragColor = vec4(pixelColor * 0.3 + previousPixel.xyz,1.0);
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