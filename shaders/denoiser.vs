#version 410 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec2 inTexCoords;

out vec2 texCoords;

void main(){
    texCoords = vec2(inTexCoords.x,inTexCoords.y*-1);
    gl_Position = vec4(position.x,position.y,0.0,1.0);
}