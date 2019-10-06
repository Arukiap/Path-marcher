#ifndef SHADER_H
#define SHADER_H

#include <string>
#include <fstream>
#include <iostream>
#define GLEW_STATIC
#include <GL/glew.h>


class Shader{
    public:
        Shader(const std::string& fileName);
        void Bind(unsigned const int, unsigned const int displayWidth, unsigned const int displayHeight);
        virtual ~Shader();
    private:
        static const unsigned int NUM_SHADERS = 2; //Vertex and Fragment shader
        GLuint program;
        GLuint shaders[NUM_SHADERS];
};


#endif // SHADER_H