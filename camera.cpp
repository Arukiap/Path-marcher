#include "camera.h"
#include <iostream>

Camera::Camera(glm::vec3 position, glm::vec3 front, glm::vec3 up, float yaw, float pitch, float fov, float speed){
    m_position = position;
    m_front = front;
    m_up = up;
    m_yaw = yaw;
    m_pitch = pitch;
    m_fov = fov;
    m_speed = speed;
    m_currentMouseX=0;
    m_currentMouseY=0;
}


glm::mat4 Camera::getViewTransformation(){
    return glm::lookAt(m_position,m_position + m_front, m_up);
}

void Camera::moveFront(){
    m_position += m_speed * m_front;
}

void Camera::moveBack(){
    m_position -= m_speed * m_front;
}

void Camera::moveLeft(){
    m_position -= glm::normalize(glm::cross(m_front,m_up)) * m_speed;
}

void Camera::moveRight(){
    m_position += glm::normalize(glm::cross(m_front,m_up)) * m_speed;
}

void Camera::moveUp(){
    m_position += m_up * m_speed;
}

void Camera::moveDown(){
    m_position -= m_up * m_speed;
}

void Camera::zoom(int zoomDirection, float zoomFactor){
    m_fov -= zoomDirection*zoomFactor;
}

void Camera::updateYawAndPitch(float sensitivity){
    m_yaw += m_currentMouseX * sensitivity;
    m_pitch -= m_currentMouseY * sensitivity;

    //In order to avoid screen getting flipped we must clamp the pitch value
    if(m_pitch > 89.0f){
        m_pitch = 89.0f;
    }
    if(m_pitch < -89.0f){
        m_pitch = -89.0f;
    }

    glm::vec3 front;
    front.x = cos(glm::radians(m_yaw)) * cos(glm::radians(m_pitch));
    front.y = sin(glm::radians(m_pitch));
    front.z = sin(glm::radians(m_yaw)) * cos(glm::radians(m_pitch));

    glm::vec3 right;
    right.x = sin(glm::radians(m_yaw));
    right.y = 0.0;
    right.z = -cos(glm::radians(m_yaw));

    m_front = glm::normalize(front);
    m_up = glm::cross(front,right);
}

void Camera::updateSpeed(float speed){
    m_speed = speed;
}