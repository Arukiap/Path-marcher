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

struct Sphere {
	vec3 center;
	float radius;
	vec3 albedo; //color of the object 
	double emission; //if non 0 it is a light source
	int type; //diffuse 0, specular 1 or refractive 2
};

struct Collision {
	float distance;
	vec3 color;
};

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
 * Signed distance function of a plane
 */ 
Collision planeSDF(vec3 samplePoint){
	return Collision(samplePoint.y,vec3(1.0,1.0,1.0));
}

/*
 * Signed distance function of a sphere
 */ 
Collision sphereSDF(vec3 samplePoint, Sphere sphere){
	return Collision(length(samplePoint-sphere.center) - sphere.radius, sphere.albedo);
}

/*
 * Defines the current scene through signed distance functions
 */
Collision sceneSDF(vec3 samplePoint){

	const Sphere spheres[3] = Sphere[3](
		Sphere(vec3(0.0,0.2,0.0),0.2,vec3(0.0,1.0,0.0),0.0,0),
		Sphere(vec3(0.6,0.3,0.0),0.3,vec3(1.0,0.2,0.6),0.0,0),
		Sphere(vec3(1.4,0.4,0.0),0.4,vec3(.3,0.4,0.8),0.0,0)
	);

	Collision minimum = Collision(MAX_DIST,vec3(0.0,0.0,0.0));

	for(int i = 0; i<spheres.length(); i++){
		Collision sphereCollision = sphereSDF(samplePoint,spheres[i]);
		Collision planeCollision = planeSDF(samplePoint);
		if(minimum.distance > sphereCollision.distance){
			minimum = sphereCollision;
		}
		if(minimum.distance > planeCollision.distance){
			minimum = planeCollision;
		}
	}
	return minimum;
}


float SDFBox( vec3 p, vec3 b )
{
  vec3 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

/*
 * Ray marching algorithm.
 * Returns aprox. distance to the scene from a certain point with a certain direction.
 */
Collision rayMarch(vec3 from, vec3 direction) {
	float totalDistance = 0.0;
	Collision sceneCollision;
	for (int steps = 0; steps < MAX_MARCHING_STEPS; steps++){
		vec3 ray = from + totalDistance * direction;
		sceneCollision = sceneSDF(ray);
		totalDistance += sceneCollision.distance;
		if(sceneCollision.distance > MAX_DIST || sceneCollision.distance < EPSILON) break;
	}
	return Collision(totalDistance,sceneCollision.color);
}

vec3 rayDirection(float fov, vec2 size, vec2 fragCoord, mat3 cameraMatrix){
    vec2 xy = fragCoord - size / 2.0;
    float z = size.y / tan(radians(fov)/2.0);
    return ( cameraMatrix * normalize(vec3(xy,z)));
}

void main(){	

    vec3 direction = rayDirection(v_fov,v_resolution,gl_FragCoord.xy, v_cameraMatrix);
    vec3 eye = v_cameraPosition;

    Collision sceneCollision = rayMarch(eye,direction);

		float rnd = random( gl_FragCoord.xy/v_resolution.xy );

		vec3 hemisphereSample = sampleHemisphere(rnd,fract(rnd/10));

    //gl_FragColor = vec4(vec3(marchedDistance/20),1.0);
		gl_FragColor = vec4(sceneCollision.color/sceneCollision.distance,1.0);
} 