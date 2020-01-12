#include <iostream>
#define GLEW_STATIC
#include <GL/glew.h>
#include "display.h"
#include "mesh.h"
#include "shader.h"
#include "camera.h"


//System resolution in pixels
#define SCREEN_WIDTH 1280
#define SCREEN_HEIGHT 720

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
    Vertex vertices[] = { Vertex(glm::vec3(-1.0,1.0,0),glm::vec2(0.0,0.0)),
                          Vertex(glm::vec3(1.0,1.0,0),glm::vec2(1.0,0.0)),
                          Vertex(glm::vec3(-1.0,-1.0,0),glm::vec2(0.0,1.0)),
                          Vertex(glm::vec3(1.0,-1.0,0.0),glm::vec2(1.0,1.0))};

    //Vertex vertices[] =
	//{
	//	Vertex(glm::vec3(-0.5, -0.5, -1), glm::vec2(1, 0)),
	//	Vertex(glm::vec3(-0.5, 0.5, -1), glm::vec2(0, 0)),
    //	Vertex(glm::vec3(0.5, 0.5, -1), glm::vec2(0, 1)),
	//};

    Mesh mesh(vertices,sizeof(vertices)/sizeof(vertices[0]));

    //add into display class
    Camera camera(glm::vec3(1.0f,0.5f,2.0f),glm::vec3(0.0f,0.0f,1.0f),glm::vec3(0.0f,1.0f,0.0f),-90.0f,0.0f,120.0f,0.01f);  
    
    //Create model matrix
    glm::mat4 model = glm::mat4(1.0f);

    //Create resolution vector to pass to shader            
    glm::vec2 resolution = glm::vec2(SCREEN_WIDTH,SCREEN_HEIGHT);
    
    //shader.setMat4("model",model);
    shader.setVec2("resolution",resolution);

    while(!display.IsClosed()){   
        display.Clear(0.0f,0.15f,0.3f,1.0f);
        //Create projection matrix
        //glm::mat4 projection = glm::perspective(glm::radians(camera.getFov()),(float)SCREEN_WIDTH / (float)SCREEN_HEIGHT, 0.1f, 100.0f);
        shader.setVec3("cameraPosition",camera.getPosition());
        shader.setVec3("cameraUp",camera.getUp());
        shader.setVec3("cameraFront",camera.getFront());
        shader.setFloat("fov",camera.getFov());
        shader.setFloat("time",(float)SDL_GetTicks());
        //shader.setMat4("projection",projection);
        //shader.setMat4("view",camera.getViewTransformation());
        mesh.Draw();
        display.ListenInput(&camera);
        display.Update();
    }

    return 0;
}