#version 410 core

/*
 * Fragment Shader - Where the magic happens
 */

in vec4 gl_FragCoord;

varying float vSystemTime;
varying vec2 vSystemResolution;

void main(){	
	float redValue = sin(vSystemTime*0.005);
	gl_FragColor = vec4(redValue,0.0,1.0,1.0);
}