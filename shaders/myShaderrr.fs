#version 410 core

in vec4 gl_FragCoord;

varying vec2 v_resolution;
varying float v_fov;
varying mat3 v_cameraMatrix;
varying vec3 v_cameraPosition;

//Ray Marching constants
const int MAX_MARCHING_STEPS = 128;
const float MIN_DIST = 0.0;
const float MAX_DIST = 5.0;
const float EPSILON = 0.0005;
const int OCCLUSION_SAMPLES = 5;

//Represents an object
struct Object {
	vec3 center;
	float size;
	int type; //0 for sphere, 1 for hollow cube, 2 for cone
	vec3 albedo; //color of the object
	int surfaceType; //diffuse 0, specular 1, refractive 2 or "reflective" 3
	int id; //object id
};

//Represents the march of a ray
struct March {
	float distance; //distance of the current march
	vec3 color; //color of the object it hit
	int objectId; //id of the object it hit
};

//Scene constants
const int numberOfSceneObjects = 4;
const vec3 sceneBackgroundColor = vec3(0.2,0.6,1.0);
const vec3 lightSource = vec3(-2.0,9.0,3.0);
//const vec3 lightSource = vec3(1.0,2.0,0.0);

//Represents the current scene
struct Scene {
	Object sceneObjects[numberOfSceneObjects];
};

//Scene instantiation
const Scene globalScene = Scene(Object[numberOfSceneObjects](
	Object(vec3(0.0,1.5,0.0),1.5,1,vec3(0.8,0.8,0.8),0,0), //plane -> the "room" of the scene
	Object(vec3(0.0,.35,0.0),0.7,0,vec3(0.1,0.2,0.6),0,1), //sphere -> one of the objects in the scene
	Object(vec3(1.0,0.6,0.0),0.7,2,vec3(0.9,0.0,0.0),0,2), //cone -> one of the objects in the scene
	Object(vec3(2.0,0.5,0.0),0.7,3,vec3(0.9,0.9,0.0),0,2) //solid cube -> one of the objects in the scene
));






/* /////////////////////////////
 * // Object Distance Getters //
 *//////////////////////////////

//id 0 - Sphere
float getSphereDistance(vec3 tracePoint, vec3 center, float radius){
	return length(tracePoint-center) - radius;
}

//id 1 - Plane
float getPlaneDistance(vec3 tracePoint){
	return tracePoint.y;
}

//id 2 - Cone
float getConeDistance( vec3 tracePoint )
{
	vec3 c = vec3(0.8,0.6,1.0);
    vec2 q = vec2( length(tracePoint.xz), tracePoint.y );
    float d1 = -tracePoint.y-c.z;
    float d2 = max( dot(q,c.xy), q.y);
    return length(max(vec2(d1,d2),0.0)) + min(max(d1,d2), 0.);
}

//id 3 - Solid Cube
float getSolidCubeDistance( vec3 tracePoint, vec3 center )
{
  vec3 b = vec3(0.2,0.5,0.2);
  vec3 q = abs(tracePoint-center) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}





/* /////////////////////////////
 * // Scene Distance Methods  //
 *//////////////////////////////

March getMarchDistanceToObject(vec3 ray, Object object){
	switch (object.type) {
		case 0: //sphere
				return March(getSphereDistance(ray,object.center,object.size/2),object.albedo,object.id);
		case 1: //hollow cube
				return March(getPlaneDistance(ray),object.albedo,object.id);
		case 2: //cone
				return March(getConeDistance(ray-object.center),object.albedo,object.id);
		case 3: //solid sube
				return March(getSolidCubeDistance(ray,object.center),object.albedo,object.id);
	}
	return March(MAX_DIST,sceneBackgroundColor,-1);
}

March marchToClosestObject(vec3 ray){

	March closestCollision = March(MAX_DIST,sceneBackgroundColor,-1);

	for(int i = 0; i < globalScene.sceneObjects.length(); i++){
		March currentCollision = getMarchDistanceToObject(ray,globalScene.sceneObjects[i]);
		if(closestCollision.distance > currentCollision.distance){
			closestCollision = currentCollision;
		}
	}
	return closestCollision;
}

March rayMarchScene(vec3 from, vec3 direction) {
	float totalDistance = 0.0;
	March sceneCollision;
	for (int steps = 0; steps < MAX_MARCHING_STEPS; steps++){
		vec3 ray = from + totalDistance * direction;
		sceneCollision = marchToClosestObject(ray);
		totalDistance += sceneCollision.distance;
		if(sceneCollision.distance > MAX_DIST || sceneCollision.distance < EPSILON*0.01) break;
	}
	return March(totalDistance,sceneCollision.color,sceneCollision.objectId);
}













 

/*
 * Returns an aprox. normal vector a given surface point.
 */
vec3 getNormal(vec3 surfacePoint){
	vec2 e = vec2(.001,0); //epsilon vector
	vec3 normal = vec3(
		marchToClosestObject(surfacePoint+e.xyy).distance - marchToClosestObject(surfacePoint-e.xyy).distance,
		marchToClosestObject(surfacePoint+e.yxy).distance - marchToClosestObject(surfacePoint-e.yxy).distance,
		marchToClosestObject(surfacePoint+e.yyx).distance - marchToClosestObject(surfacePoint-e.yyx).distance
    );
	return normalize(normal);
}

float getDiffuse(vec3 surfacePoint, vec3 normal){
	vec3 directionToLightSource = lightSource - surfacePoint;
	return clamp(dot(normal,directionToLightSource),0.0,1.0);
}

float getSpecular(vec3 surfacePoint, vec3 rayDirection, vec3 normal){
	vec3 lightToObjectDirection = normalize(lightSource-surfacePoint);
	vec3 r = reflect(lightToObjectDirection,normal);
	return pow(max(dot(r,rayDirection),0.0),16);
}

float getHardShadow(vec3 surfacePoint, vec3 normal){
	vec3 newPoint = surfacePoint+normal*EPSILON;
	vec3 directionToLightSource = normalize(lightSource-newPoint);
    float distanceToLightSource = rayMarchScene(newPoint,directionToLightSource).distance; 
	if( distanceToLightSource < length(directionToLightSource)){
		return 0.1;
	}
    return 1.0;
}

float getSoftShadow(vec3 surfacePoint, vec3 normal){
	vec3 origin = surfacePoint + normal * EPSILON * 4;
	vec3 direction = normalize(lightSource-origin);
	float shadowValue = 1.0;

	for( float t = 0.0; t < MAX_DIST;){
		float distanceToScene = marchToClosestObject(origin+direction*t).distance;
		if(distanceToScene < 0.001){
			return 0.0;
		}
		shadowValue = min(shadowValue,2*distanceToScene/t);
		t += distanceToScene;
	}

	return shadowValue;
}

vec3 getReflection(vec3 surfacePoint, vec3 rayDirection, vec3 normal){
	vec3 reflectionDirection = reflect(rayDirection,normal);
	vec3 color = rayMarchScene(surfacePoint+normal*EPSILON*2,reflectionDirection).color;
	return color;
}

float getAmbientOcclusion(vec3 surfacePoint, vec3 normal){
	float sampledDistance = 0;
	float occlusion = 0;
	float sampleDistance = 0.1;
	for(int k = 1; k<OCCLUSION_SAMPLES; k++){
		sampledDistance = marchToClosestObject(surfacePoint+normal*sampleDistance).distance;
		occlusion += 1/pow(2,k)*(k*sampleDistance-sampledDistance);
	}
	return 1.0 - clamp(2.0*occlusion,0.0,1.0);
}

vec3 computeRayDirection(float fov, vec2 size, vec2 fragCoord, mat3 cameraMatrix){
    vec2 xy = fragCoord - size / 2.0;
    float z = size.y / tan(radians(fov)/2.0);
    return ( cameraMatrix * normalize(vec3(xy,z)));
}

void main(){
    vec3 rayDirection = computeRayDirection(v_fov,v_resolution,gl_FragCoord.xy, v_cameraMatrix);
	March marchToScene = rayMarchScene(v_cameraPosition,rayDirection);
	vec3 hitPoint = v_cameraPosition + rayDirection * marchToScene.distance;
	vec3 normal = getNormal(hitPoint);
	vec3 colorReflection = getReflection(hitPoint,rayDirection,normal);
	float diffuse = getDiffuse(hitPoint,normal);
	float shadow = getSoftShadow(hitPoint,normal);
	vec3 reflectionColor = getReflection(hitPoint,rayDirection,normal);
	float ambientOcclusion = getAmbientOcclusion(hitPoint,normal);
	float specular = getSpecular(hitPoint,rayDirection,normal);

	if(marchToScene.distance >= MAX_DIST){
		gl_FragColor = vec4(sceneBackgroundColor,1.0);
	} else {
		vec3 finalColor = marchToScene.color*(diffuse+0.5)*clamp(shadow,0.3,1.0)*(ambientOcclusion-0.1)+(specular*0.4);
		vec3 reflection = mix(finalColor,reflectionColor,marchToScene.distance/MAX_DIST-0.2);
		gl_FragColor = vec4(reflection,1.0);
		//gl_FragColor = vec4(hitPoint,1.0);
	}
	

	
} 