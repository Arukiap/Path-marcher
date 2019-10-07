#include "camera.h"

Camera::Camera(glm::vec3 position, glm::vec3 front, glm::vec3 up, float yaw, float pitch, float fov){
    m_position = position;
    m_front = front;
    m_up = up;
    m_yaw = yaw;
    m_pitch = pitch;
    m_fov = fov;
}


glm::mat4 Camera::getViewTransformation(){
    return glm::lookAt(m_position,m_position + m_front, m_up);
}