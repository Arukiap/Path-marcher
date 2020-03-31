#include "shader.h"
#include "glm/gtc/type_ptr.hpp"
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

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

void Shader::loadTexture(const GLchar* pathname, const GLchar* name){
    glGenTextures(1, &this->loadedTexture);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D,this->loadedTexture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_MIRRORED_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_MIRRORED_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    int width, height, nrChannels;
    unsigned char *data = stbi_load(pathname, &width, &height, &nrChannels, 0); 

    if(data){
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
        glUseProgram(program);
        GLint uniformLocation = glGetUniformLocation(program,name);
        glUniform1i(uniformLocation,0);
    } else {
        std::cout << "Failed to load texture" << std::endl;
    }
    stbi_image_free(data);
}

void Shader::createRenderTarget(const int screenWidth, const int screenHeight){
    //Create Frame buffer object:
    glGenFramebuffers(1,&this->framebuffer);
    glBindFramebuffer(GL_FRAMEBUFFER,this->framebuffer);

    //Create empty OUTPUT texture which will contain the RGB output of the shader
    glGenTextures(1,&this->outputTexture);
    glActiveTexture(GL_TEXTURE0 + 1);
    glBindTexture(GL_TEXTURE_2D, this->outputTexture);
    glTexImage2D(GL_TEXTURE_2D, 0,GL_RGB, screenWidth, screenHeight, 0,GL_RGB, GL_UNSIGNED_BYTE, 0);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glUseProgram(program);
    glFramebufferTexture(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, this->outputTexture, 0);
    glDrawBuffer(GL_COLOR_ATTACHMENT0);
}

void Shader::createInputTarget(const int screenWidth, const int screenHeight){
    //Create empty texture which will contain the RGB input of the shader
    glGenTextures(1,&this->inputTexture);
    glActiveTexture(GL_TEXTURE0 + 2);
    glBindTexture(GL_TEXTURE_2D, this->inputTexture);
    glTexImage2D(GL_TEXTURE_2D, 0,GL_RGB, screenWidth, screenHeight, 0,GL_RGB, GL_UNSIGNED_BYTE, 0);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    GLint uniformLocation = glGetUniformLocation(program,"inputTexture");
    glUniform1i(uniformLocation,1);
}

void Shader::copyOutputToInputTexture(const int screenWidth, const int screenHeight){
    glCopyTextureSubImage2D(this->outputTexture,0,0,0,0,0,screenWidth,screenHeight);
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