#version 410 core

in vec4 gl_FragCoord;

varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying mat3 v_cameraMatrix;
varying float v_fov;
varying float v_time;

//Ray Marching constants
const int MAX_MARCHING_STEPS = 128;
const float MIN_DIST = 0.0;
const float MAX_DIST = 100.0;
const float EPSILON = 0.00001;

const vec3 lightSource = vec3(-0.2,0.0,1.0);
const vec3 albedo = vec3(0.4,0.8,1.0);


float twistedCubeDistance(vec3 currentPoint, vec3 center, float sideLength){
    const float k = 1.5;
    float c = cos(k*currentPoint.y);
    float s = sin(k*currentPoint.y);
    mat2 m = mat2(c,-s,s,c);
    vec3 twistedPoint = vec3(m*currentPoint.xz,currentPoint.y);
	vec3 q = abs(twistedPoint-center) - vec3(sideLength);
	return (length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0))-0.05;
}

float sceneSDF(vec3 currentPoint){
    return twistedCubeDistance(currentPoint,vec3(0.0,0.0,0.0),0.5);
}

/*
 * Ray marching algorithm.
 * Returns aprox. distance to the scene from a certain point with a certain direction.
 */
float rayMarch(vec3 from, vec3 direction) {
	float totalDistance = 0.0;
	for (int steps = 0; steps < MAX_MARCHING_STEPS; steps++){
		vec3 ray = from + totalDistance * direction;
		float distanceToScene = sceneSDF(ray);
		totalDistance += distanceToScene;
		if(distanceToScene > MAX_DIST || distanceToScene < EPSILON) break;
	}
	return totalDistance;
}

vec3 rayDirection(float fov, vec2 size, vec2 fragCoord, mat3 cameraMatrix){
    vec2 xy = fragCoord - size / 2.0;
    float z = size.y / tan(radians(fov)/2.0);
    return ( cameraMatrix * normalize(vec3(xy,z)));
}

/*
 * Returns an aprox. normal vector a given surface point.
 */
vec3 getNormal(vec3 surfacePoint){
	float distanceToPoint = sceneSDF(surfacePoint);
	vec2 e = vec2(.001,0); //epsilon vector
	vec3 normal = vec3(
        sceneSDF(surfacePoint+e.xyy) - sceneSDF(surfacePoint-e.xyy),
        sceneSDF(surfacePoint+e.yxy) - sceneSDF(surfacePoint-e.yxy),
        sceneSDF(surfacePoint+e.yyx) - sceneSDF(surfacePoint-e.yyx)
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
	return pow(max(dot(r,rayDirection),0.0),4);
}

void main(){	
    vec3 direction = rayDirection(v_fov,v_resolution,gl_FragCoord.xy, v_cameraMatrix);
    float distanceToScene = rayMarch(v_cameraPosition,direction);

    if(distanceToScene < MAX_DIST){
        vec3 hitPoint = v_cameraPosition + direction * distanceToScene;
        vec3 normal = getNormal(hitPoint);
        float diffuse = getDiffuse(hitPoint,normal);
        float specular = getSpecular(hitPoint,direction,normal);
        vec3 color = albedo * (diffuse + 0.3) + (specular*0.5);
        gl_FragColor = vec4(color,0.0);
    } else {
        gl_FragColor = vec4(0.0,0.0,0.0,0.0);
    }
}