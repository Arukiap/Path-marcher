#include <iostream>
#define GLEW_STATIC
#include <GL/glew.h>
#include "display.h"
#include "mesh.h"
#include "shader.h"


//System resolution in pixels
#define WIDTH 500
#define HEIGHT 500

#ifdef _WIN32
#define SEPARATOR "\\"
#else
#define SEPARATOR "/"
#endif

int main(int argc, char* argv[]){

    Display display(WIDTH,HEIGHT,"My shader application");

    Shader shader("." SEPARATOR "shaders" SEPARATOR "myShader");

    //Create 2D mesh that occupies the whole screen for fragment shader to draw on
    Vertex vertices[] = { Vertex(glm::vec3(-1.0,1.0,0),glm::vec2(0.0,0.0)),
                          Vertex(glm::vec3(1.0,1.0,0),glm::vec2(1.0,0.0)),
                          Vertex(glm::vec3(-1.0,-1.0,0),glm::vec2(0.0,1.0)),
                          Vertex(glm::vec3(1.0,-1.0,0.0),glm::vec2(1.0,1.0))};

    Mesh mesh(vertices,sizeof(vertices)/sizeof(vertices[0]));

    while(!display.IsClosed()){
        display.Clear(0.0f,0.15f,0.3f,1.0f);
        shader.Bind(SDL_GetTicks(),WIDTH,HEIGHT); //Binds all the variables you want to share with the GPU.
        mesh.Draw();
        display.ListenInput();
        display.Update();
    }

    return 0;
}