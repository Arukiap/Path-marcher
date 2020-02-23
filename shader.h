#ifndef SHADER_H
#define SHADER_H

#include <string>
#include <fstream>
#include <iostream>
#define GLEW_STATIC
#include <GL/glew.h>
#include <glm/glm.hpp>


class Shader{
    public:
        Shader(const std::string& fileName);
        void setInt(const GLchar* name, unsigned const int value);
        void setFloat(const GLchar* name, const float value);
        void setMat4(const GLchar* name, glm::mat4 value);
        void setVec2(const GLchar* name, glm::vec2 value);
        void setVec3(const GLchar* name, glm::vec3 value);
        void loadTexture(const GLchar* pathname, const GLchar* name);
        void useTexture(GLuint *inputTexture);
        void createRenderTarget(const int screenWidth, const int screenHeight);
        void createInputTarget(const int screenWidth, const int screenHeight);
        void copyOutputToInputTexture(const int screenWidth, const int screenHeight);
        void use(){
            glUseProgram(this->program);
        }
        GLuint getProgram(){
            return this->program;
        }
        GLuint getFrameBuffer(){
            return this->framebuffer;
        }
        GLuint getInputTexture(){
            return this->inputTexture;
        }
        GLuint getOutputTexture(){
            return this->outputTexture;
        }
        GLuint getLoadedTexture(){
            return this->loadedTexture;
        }
        virtual ~Shader();
    private:
        static const unsigned int NUM_SHADERS = 2; //Vertex and Fragment shader
        GLuint program;
        GLuint framebuffer;
        GLuint outputTexture;
        GLuint inputTexture;
        GLuint loadedTexture;
        GLuint shaders[NUM_SHADERS];
};


#endif // SHADER_H