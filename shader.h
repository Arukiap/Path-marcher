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
        void setMat4(const GLchar* name, glm::mat4 value);
        virtual ~Shader();
    private:
        static const unsigned int NUM_SHADERS = 2; //Vertex and Fragment shader
        GLuint program;
        GLuint shaders[NUM_SHADERS];
};


#endif // SHADER_H