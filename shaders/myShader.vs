#version 410 core

layout (location = 0) in vec4 position;

uniform vec2 resolution;
uniform vec3 cameraPosition;
uniform vec3 cameraFront;
uniform vec3 cameraUp;
uniform float fov;

varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying mat3 v_cameraMatrix;
varying float v_fov;

void main(){
    gl_Position = position;
    vec3 cameraRight = cross(cameraFront,cameraUp);
    v_cameraMatrix = mat3(cameraRight,cameraUp,cameraFront);
    v_resolution = resolution;
    v_cameraPosition = cameraPosition;
    v_fov = fov;
}