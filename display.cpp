#include "display.h"
#define GLEW_STATIC
#include "GL/glew.h"
#include <iostream>

Display::Display(int width, int height, const std::string& title){

    SDL_Init(SDL_INIT_EVERYTHING);

    //32 bit color + transparency
    SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_ALPHA_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_BUFFER_SIZE, 32);

    //Enable double buffering
    SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);

    window =  SDL_CreateWindow(title.c_str(),SDL_WINDOWPOS_CENTERED,SDL_WINDOWPOS_CENTERED,width,height,SDL_WINDOW_OPENGL);

    //Hide mouse cursor and block it to middle of the screen
    SDL_ShowCursor(SDL_DISABLE);
    SDL_SetRelativeMouseMode(SDL_TRUE);
    
    //GPU connects directly to the window, instead of being the OS in complete command of the window
    glContext = SDL_GL_CreateContext(window);

    GLenum status = glewInit();

    if(status != GLEW_OK){
        std::cerr << "Glew failed to initialize." << std::endl;
    }

    isClosed = false;
}

Display::~Display(){
    SDL_GL_DeleteContext(glContext);
    SDL_DestroyWindow(window);
    SDL_Quit();
}

void Display::Clear(float r, float g, float b, float a){
    glClearColor(r,g,b,a);
    glClear(GL_COLOR_BUFFER_BIT);
}

//Listens to keyboard and mouse input and returns true if keyboard or mouse were clicked
bool Display::ListenInput(Camera *camera){
    SDL_Event e;

    bool hasReceivedAnyInput = false;

    const Uint8 *keystate = SDL_GetKeyboardState(NULL);

    int previous_mouseX = camera->m_currentMouseX, previous_mouseY = camera->m_currentMouseY;

    const Uint32 mousestate = SDL_GetRelativeMouseState(&camera->m_currentMouseX,&camera->m_currentMouseY);

    if((previous_mouseX != camera->m_currentMouseX) || (previous_mouseY != camera->m_currentMouseY)) hasReceivedAnyInput = true;

    camera->updateYawAndPitch(0.1f);

    if(keystate[SDL_SCANCODE_W]){
        camera->moveFront();  
        hasReceivedAnyInput = true;  
    }

    if(keystate[SDL_SCANCODE_S]){
        camera->moveBack();   
        hasReceivedAnyInput = true;
    }

    if(keystate[SDL_SCANCODE_A]){
        camera->moveLeft();  
        hasReceivedAnyInput = true;   
    }

    if(keystate[SDL_SCANCODE_D]){
        camera->moveRight();   
        hasReceivedAnyInput = true; 
    }

    if(keystate[SDL_SCANCODE_SPACE]){
        camera->moveUp();   
        hasReceivedAnyInput = true; 
    }

    if(keystate[SDL_SCANCODE_LCTRL]){
        camera->moveDown();   
        hasReceivedAnyInput = true; 
    }

     while(SDL_PollEvent(&e)){
        switch( e.type ){
            case SDL_QUIT:
                isClosed = true;
                break;
            case SDL_MOUSEWHEEL:
                camera->zoom(e.wheel.y,5);
                hasReceivedAnyInput = true; 
                break;
            default: break;
        }
    }

    return hasReceivedAnyInput;
}

bool Display::IsClosed(){
    return isClosed;
}

void Display::Update(){
    SDL_GL_SwapWindow(window);
}



