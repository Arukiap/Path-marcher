#version 410 core

/*
 * Vertex Shader - Only passes shared values from CPU to the fragment shader. 
 */

attribute vec3 pos;

varying float vSystemTime;
varying vec2 vSystemResolution;

uniform float systemTime;
uniform vec2 systemResolution;

void main(){
    gl_Position = vec4(pos,1.0);
    vSystemTime = systemTime;
    vSystemResolution = systemResolution;
}