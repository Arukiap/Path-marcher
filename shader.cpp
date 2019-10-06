#include "shader.h"

static GLuint CreateShader(const std::string& text, GLenum shaderType);
static std::string LoadShader(const std::string& fileName);
static void CheckShaderError(GLuint shader, GLuint flag, bool isProgram, const std::string& errorMessage);

Shader::Shader(const std::string& fileName){

    program = glCreateProgram();
    shaders[0] = CreateShader(LoadShader(fileName + ".vs"),GL_VERTEX_SHADER);
    shaders[1] = CreateShader(LoadShader(fileName + ".fs"),GL_FRAGMENT_SHADER);

    for(unsigned int i = 0; i < NUM_SHADERS; i++)
        glAttachShader(program,shaders[i]);

    glBindAttribLocation(program,0,"pos");
    glBindAttribLocation(program,1,"texCoord");

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

/*
 * Binds all the CPU values to GPU variables.
 * If you want to share any value with the shader program, this function is where to do so.
 * This function is called once every frame.
 */
void Shader::Bind(unsigned const int timeTicks, unsigned const int displayWidth, unsigned const int displayHeight){

    glUseProgram(program);

    //Pass display resolution as 2D vector to shaders
    GLint resolutionUniformLocation = glGetUniformLocation(program,"systemResolution");
    glUniform2f(resolutionUniformLocation,displayWidth,displayHeight);

    //Pass system time (miliseconds since start of application) to shaders
    GLint timeUniformLocation = glGetUniformLocation(program,"systemTime");
    glUniform1f(timeUniformLocation,timeTicks);
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