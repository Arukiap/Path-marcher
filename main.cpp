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

#define CAMERA_SPEED 0.001


#ifdef _WIN32
#define SEPARATOR "\\"
#else
#define SEPARATOR "/"
#endif



int main(int argc, char* argv[]){

    glEnable(GL_DEPTH_TEST); //Render objects correctly on top of each other

    Display display(SCREEN_WIDTH,SCREEN_HEIGHT,"Path marcher");

    Shader denoiser("." SEPARATOR "shaders" SEPARATOR "denoiser");
    Shader pathTracer("." SEPARATOR "shaders" SEPARATOR "pathTracer");

    //Create 2D quad mesh that occupies the whole screen for fragment shader to draw on
    Vertex vertices[] = { Vertex(glm::vec3(-1.0,1.0,0),glm::vec2(0.0,0.0)),
                          Vertex(glm::vec3(1.0,1.0,0),glm::vec2(1.0,0.0)),
                          Vertex(glm::vec3(-1.0,-1.0,0),glm::vec2(0.0,1.0)),
                          Vertex(glm::vec3(1.0,-1.0,0.0),glm::vec2(1.0,1.0))};

    Mesh mesh(vertices,sizeof(vertices)/sizeof(vertices[0]));

    Camera camera(glm::vec3(1.0f,0.5f,2.0f),glm::vec3(0.0f,0.0f,1.0f),glm::vec3(0.0f,1.0f,0.0f),-90.0f,0.0f,120.0f,CAMERA_SPEED);  

    //Create resolution vector to pass to shader            
    glm::vec2 resolution = glm::vec2(SCREEN_WIDTH,SCREEN_HEIGHT);  
    pathTracer.setVec2("resolution",resolution);

    pathTracer.loadTexture("blue_noise.png","blueNoise");

    //Specify output and input targets for the path tracing shader to write and read from.
    pathTracer.createRenderTarget(SCREEN_WIDTH,SCREEN_HEIGHT);
    pathTracer.createInputTarget(SCREEN_WIDTH,SCREEN_HEIGHT);

    //Following variables are used in order to calculate current frames per second and change camera behaviour speed based on ellapsed time between frames
    float initialTime = (float)SDL_GetTicks();
    float startClock = 0;
    float deltaClock = 0;
    int currentFps = 0;

    while(!display.IsClosed()){   

        //Listen to input
        bool hasCameraChanged = display.ListenInput(&camera);
        
        //Bind frame buffer with memory texture attached
        glBindFramebuffer(GL_FRAMEBUFFER,pathTracer.getFrameBuffer());
        glEnable(GL_DEPTH_TEST);

        //Clear framebuffer content
        display.Clear(0.0f,0.0f,0.0f,0.0f);
        
        //Use path tracer
        pathTracer.use();

        //Activate blue noise texture which is bound to location 0   
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D,pathTracer.getLoadedTexture());

        //Activate input texture which is bound to location 1 inputTexture
        glActiveTexture(GL_TEXTURE0 + 1);
        glBindTexture(GL_TEXTURE_2D,pathTracer.getInputTexture());

        //Pass camera parameters to path tracer shader through the use of uniforms
        pathTracer.setVec3("cameraPosition",camera.getPosition());
        pathTracer.setVec3("cameraUp",camera.getUp());
        pathTracer.setVec3("cameraFront",camera.getFront());
        pathTracer.setFloat("fov",camera.getFov());
        pathTracer.setFloat("time",startClock);
        pathTracer.setFloat("hasCameraChanged",hasCameraChanged ? 1.0f : 0.0f);

        //Draw a quad displaying the path tracer output
        mesh.Draw();
        
        //Copy stored memory texture to the input texture of the path tracer
        glBindTexture(GL_TEXTURE_2D,pathTracer.getInputTexture());
        glCopyTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, 0, 0, SCREEN_WIDTH, SCREEN_HEIGHT, 0);
        
        //Bind back to default framebuffer
        glBindFramebuffer(GL_FRAMEBUFFER,0);
        glDisable(GL_DEPTH_TEST);

        display.Clear(0.0f,0.15f,0.3f,1.0f);

        //Start using denoiser shader and draw the memory texture to the user screen
        denoiser.use();
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D,pathTracer.getOutputTexture());
        mesh.Draw();

        deltaClock = SDL_GetTicks() - startClock;
        startClock = SDL_GetTicks();

        if(deltaClock != 0){
            currentFps = (int)(1000/deltaClock);
            camera.updateSpeed(CAMERA_SPEED*deltaClock);
        }

        printf("FPS: %d\n",currentFps);

        display.Update();
    }

    return 0;
}