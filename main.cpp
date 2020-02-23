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

    Shader denoiser("." SEPARATOR "shaders" SEPARATOR "denoiser");
    Shader pathTracer("." SEPARATOR "shaders" SEPARATOR "myShader");

    //Create 2D mesh that occupies the whole screen for fragment shader to draw on
    Vertex vertices[] = { Vertex(glm::vec3(-1.0,1.0,0),glm::vec2(0.0,0.0)),
                          Vertex(glm::vec3(1.0,1.0,0),glm::vec2(1.0,0.0)),
                          Vertex(glm::vec3(-1.0,-1.0,0),glm::vec2(0.0,1.0)),
                          Vertex(glm::vec3(1.0,-1.0,0.0),glm::vec2(1.0,1.0))};

    Mesh mesh(vertices,sizeof(vertices)/sizeof(vertices[0]));

    //add into display class
    Camera camera(glm::vec3(1.0f,0.5f,2.0f),glm::vec3(0.0f,0.0f,1.0f),glm::vec3(0.0f,1.0f,0.0f),-90.0f,0.0f,120.0f,0.01f);  
    
    //Create model matrix
    glm::mat4 model = glm::mat4(1.0f);

    //Create resolution vector to pass to shader            
    glm::vec2 resolution = glm::vec2(SCREEN_WIDTH,SCREEN_HEIGHT);
    
    //shader.setMat4("model",model);
    pathTracer.setVec2("resolution",resolution);

    pathTracer.loadTexture("blue_noise.png","blueNoise");

    pathTracer.createRenderTarget(SCREEN_WIDTH,SCREEN_HEIGHT);
    pathTracer.createInputTarget(SCREEN_WIDTH,SCREEN_HEIGHT);

    while(!display.IsClosed()){   
        

        //Listen to input
        bool hasCameraChanged = display.ListenInput(&camera);
        
        //Bind invisible frame buffer
        glBindFramebuffer(GL_FRAMEBUFFER,pathTracer.getFrameBuffer());
        glEnable(GL_DEPTH_TEST);

        //Clear invisible framebuffer content
        display.Clear(0.0f,0.15f,0.3f,1.0f);
        
        //Use path tracer
        pathTracer.use();

        //Activate blue noise texture which is bound to location 0 blueNoise   
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D,pathTracer.getLoadedTexture());

        //Activate input texture which is bound to location 1 inputTexture
        glActiveTexture(GL_TEXTURE0 + 1);
        glBindTexture(GL_TEXTURE_2D,pathTracer.getInputTexture());

        //Pass camera parameters to path tracer shader
        pathTracer.setVec3("cameraPosition",camera.getPosition());
        pathTracer.setVec3("cameraUp",camera.getUp());
        pathTracer.setVec3("cameraFront",camera.getFront());
        pathTracer.setFloat("fov",camera.getFov());
        pathTracer.setFloat("time",(float)SDL_GetTicks());
        pathTracer.setFloat("hasCameraChanged",hasCameraChanged ? 1.0f : 0.0f);

        //Draw a quad displaying the path tracer output
        mesh.Draw();
        
        glBindTexture(GL_TEXTURE_2D,pathTracer.getInputTexture());
        glCopyTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, 0, 0, SCREEN_WIDTH, SCREEN_HEIGHT, 0);
        
        //Bind back to default framebuffer 
        glBindFramebuffer(GL_FRAMEBUFFER,0);
        glDisable(GL_DEPTH_TEST);

        display.Clear(0.0f,0.15f,0.3f,1.0f);
        
        
        denoiser.use();
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D,pathTracer.getOutputTexture());
        mesh.Draw();
        
        display.Update();
    }

    return 0;
}