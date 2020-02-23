#version 410 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec2 inTexCoords;

layout (location = 2) out vec2 texCoords;

uniform vec2 resolution;
uniform vec3 cameraPosition;
uniform vec3 cameraFront;
uniform vec3 cameraUp;
uniform float fov;
uniform float time;
uniform float hasCameraChanged;

varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying mat3 v_cameraMatrix;
varying float v_fov;
varying float v_time;
varying float v_hasCameraChanged;

void main(){
    gl_Position = position;
    vec3 cameraRight = cross(cameraFront,cameraUp);
    v_cameraMatrix = mat3(cameraRight,cameraUp,cameraFront);
    v_resolution = resolution;
    v_cameraPosition = cameraPosition;
    v_fov = fov;
    v_time = time;
    v_hasCameraChanged = hasCameraChanged;
    texCoords = vec2(inTexCoords.x,-1*inTexCoords.y);
}