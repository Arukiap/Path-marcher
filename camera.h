#ifndef CAMERA_H
#define CAMERA_H

#include <glm/glm.hpp>
#include "glm/gtc/matrix_transform.hpp"

class Camera {
    public:
        Camera(glm::vec3 position, glm::vec3 front, glm::vec3 up, float yaw, float pitch, float fov);
        glm::mat4 getViewTransformation();
    private:
        glm::vec3 m_position, m_front, m_up;
        float m_yaw,m_pitch,m_fov;
};

#endif // CAMERA_H