#ifndef CAMERA_H
#define CAMERA_H

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>

class Camera {
    public:
        Camera(glm::vec3 position, glm::vec3 front, glm::vec3 up, float yaw, float pitch, float fov, float speed);
        glm::mat4 getViewTransformation();
        void moveFront();
        void moveBack();
        void moveLeft();
        void moveRight();
        void moveUp();
        void moveDown();
        void zoom(int zoomDirection, float zoomFactor);
        void updateYawAndPitch(float sensitivity);
        float getFov(){
            return m_fov;
        }
        glm::vec3 getPosition(){
            return m_position;
        }
        glm::vec3 getFront(){
            return m_front;
        }
        glm::vec3 getUp(){
            return m_up;
        }
        int m_currentMouseX, m_currentMouseY;
    private:
    //m_vForward
        glm::vec3 m_position, m_front, m_up;
        float m_yaw,m_pitch,m_fov,m_speed;
};

#endif // CAMERA_H