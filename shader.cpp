#include "shader.h"
#include "glm/gtc/type_ptr.hpp"

static GLuint CreateShader(const std::string& text, GLenum shaderType);
static std::string LoadShader(const std::string& fileName);
static void CheckShaderError(GLuint shader, GLuint flag, bool isProgram, const std::string& errorMessage);

Shader::Shader(const std::string& fileName){

    program = glCreateProgram();
    shaders[0] = CreateShader(LoadShader(fileName + ".vs"),GL_VERTEX_SHADER);
    shaders[1] = CreateShader(LoadShader(fileName + ".fs"),GL_FRAGMENT_SHADER);

    for(unsigned int i = 0; i < NUM_SHADERS; i++)
        glAttachShader(program,shaders[i]);

    glLinkProgram(program);
    CheckShaderError(program,GL_LINK_STATUS,true,"Error in shader, linking failed: ");   

    glValidateProgram(program);
    CheckShaderError(program,GL_VALIDATE_STATUS,true,"Error in shader, validation failed: ");   
}

Shader::~Shader(){
    for(unsigned int i = 0; i < NUM_SHADERS; i++){
        glDetachShader(program,shaders[i]);
        glDeleteShader(shaders[i]);
    }
    glDeleteProgram(program);
}

void Shader::setInt(const GLchar* name, unsigned const int value){
    glUseProgram(program);
    GLint uniformLocation = glGetUniformLocation(program,name);
    glUniform1f(uniformLocation,value);
}

void Shader::setFloat(const GLchar* name, const float value){
    glUseProgram(program);
    GLint uniformLocation = glGetUniformLocation(program,name);
    glUniform1f(uniformLocation,value);
}

void Shader::setMat4(const GLchar* name, glm::mat4 value){
    glUseProgram(program);
    GLint uniformLocation = glGetUniformLocation(program,name);
    glUniformMatrix4fv(uniformLocation,1,GL_FALSE,glm::value_ptr(value));
}

void Shader::setVec2(const GLchar* name, glm::vec2 value){
    glUseProgram(program);
    GLint uniformLocation = glGetUniformLocation(program,name);
    glUniform2f(uniformLocation,value.x,value.y);
}

void Shader::setVec3(const GLchar* name, glm::vec3 value){
    glUseProgram(program);
    GLint uniformLocation = glGetUniformLocation(program,name);
    glUniform3f(uniformLocation,value.x,value.y,value.z);
}

static GLuint CreateShader(const std::string& text, GLenum shaderType){
    GLuint shader = glCreateShader(shaderType);

    if(shader == 0)
        std::cerr << "Shader creation failed." << std::endl;
    
    const GLchar* shaderSourceStrings[1];
    GLint shaderSourceStringLengths[1];

    shaderSourceStrings[0] = text.c_str();
    shaderSourceStringLengths[0] = text.length();

    glShaderSource(shader, 1, shaderSourceStrings, shaderSourceStringLengths); //Send source code to opengl
    glCompileShader(shader);

    CheckShaderError(shader,GL_COMPILE_STATUS,false,"Error in shader compilation: ");

    return shader;
}

//Reads a shader file
static std::string LoadShader(const std::string& fileName){
    std::ifstream file;
    file.open((fileName).c_str());

    std::string output;
    std::string line;

    if(file.is_open()){
        while(file.good()){
            getline(file,line);
            output.append(line + "\n");
        }
    } else {
        std::cerr << "Unable to load shader: " << fileName << std::endl;
    }
    return output;
}

//Reports any shader errors
static void CheckShaderError(GLuint shader, GLuint flag, bool isProgram, const std::string& errorMessage){
    GLint success = 0;
    GLchar error[1024] = {0};

    if(isProgram)
        glGetProgramiv(shader,flag,&success);
    else
        glGetShaderiv(shader,flag,&success);
    
    if(success == GL_FALSE){
        if(isProgram)
            glGetProgramInfoLog(shader,sizeof(error),NULL,error);
        else
            glGetShaderInfoLog(shader,sizeof(error),NULL,error);
    }
}