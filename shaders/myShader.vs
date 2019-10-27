#version 410 core

layout (location = 0) in vec4 position;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 resolution;
uniform vec3 cameraPosition;
uniform float fov;

varying mat4 v_view;
varying mat4 v_projection;
varying vec2 v_resolution;
varying vec3 v_cameraPosition;
varying float v_fov;

void main(){
    //gl_Position = projection * model * view * position;
    gl_Position = position;
    v_view = view;
    v_projection = projection;
    v_resolution = resolution;
    v_cameraPosition = cameraPosition;
    v_fov = fov;
}