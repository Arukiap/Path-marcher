#include <iostream>
#define GLEW_STATIC
#include <GL/glew.h>
#include "display.h"
#include "mesh.h"
#include "shader.h"
#include "camera.h"


//System resolution in pixels
#define SCREEN_WIDTH 1000
#define SCREEN_HEIGHT 1000

#ifdef _WIN32
#define SEPARATOR "\\"
#else
#define SEPARATOR "/"
#endif



int main(int argc, char* argv[]){

    glEnable(GL_DEPTH_TEST); //Render objects correctly on top of each other

    Display display(SCREEN_WIDTH,SCREEN_HEIGHT,"My shader application");

    Shader shader("." SEPARATOR "shaders" SEPARATOR "myShader");

    //Create 2D mesh that occupies the whole screen for fragment shader to draw on
    Vertex vertices[] =
	{
		Vertex(glm::vec3(-0.5, -0.5, -1), glm::vec2(1, 0)),
		Vertex(glm::vec3(-0.5, 0.5, -1), glm::vec2(0, 0)),
		Vertex(glm::vec3(0.5, 0.5, -1), glm::vec2(0, 1)),
	};

    Mesh mesh(vertices,sizeof(vertices)/sizeof(vertices[0]));

    Camera camera(glm::vec3(0.0f,0.0f,3.0f),glm::vec3(0.0f,0.0f,-1.0f),glm::vec3(0.0f,1.0f,0.0f),-90.0f,0.0f,45.0f);

    //Create projection matrix
    glm::mat4 projection = glm::perspective(glm::radians(45.0f),(float)SCREEN_WIDTH / (float)SCREEN_HEIGHT, 0.1f, 100.0f);

    //Create model matrix
    glm::mat4 model = glm::mat4(1.0f);

    shader.setMat4("projection",projection);
    shader.setMat4("model",model);

    while(!display.IsClosed()){
        display.Clear(0.0f,0.15f,0.3f,1.0f);
        shader.setMat4("view",camera.getViewTransformation());
        mesh.Draw();
        display.ListenInput();
        display.Update();
    }

    return 0;
}