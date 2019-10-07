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