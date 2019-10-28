#version 410 core

in vec4 gl_FragCoord;

varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying mat3 v_cameraMatrix;
varying float v_fov;

//Ray Marching constants
const int MAX_MARCHING_STEPS = 128;
const float MIN_DIST = 0.0;
const float MAX_DIST = 100.0;
const float EPSILON = 0.01;

float SDFSphere( vec3 p, float s )
{
  return length(p)-s;
}

float SDFBox( vec3 p, vec3 b )
{
  vec3 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float sceneSDF(vec3 p){
    return max(SDFBox(p,vec3(1.0,1.0,1.0)),-SDFBox(p,vec3(0.5,0.5,1.5)));
}

/*
 * Ray marching algorithm.
 * Returns aprox. distance to the scene from a certain point with a certain direction.
 */
float rayMarch(vec3 from, vec3 direction) {
	float totalDistance = 0.0;
	int steps;
	for (steps=0; steps < MAX_MARCHING_STEPS; steps++) {
		vec3 p = from + totalDistance * direction;
		float distance = sceneSDF(p);
		totalDistance += distance;
	    if (distance > MAX_DIST || distance < EPSILON) break;
	}
	return totalDistance;
}

vec3 rayDirection(float fov, vec2 size, vec2 fragCoord, mat3 cameraMatrix){
    vec2 xy = fragCoord - size / 2.0;
    float z = size.y / tan(radians(fov)/2.0);
    return ( cameraMatrix * normalize(vec3(xy,z)));
}

void main(){	

    vec3 direction = rayDirection(v_fov,v_resolution,gl_FragCoord.xy, v_cameraMatrix);
    vec3 eye = v_cameraPosition;

    float marchedDistance = rayMarch(eye,direction);

    gl_FragColor = vec4(vec3(marchedDistance/20),1.0);
} 