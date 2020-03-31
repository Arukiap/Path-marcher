![mandelbulb](img/mandelbulb.png)

# Path marcher

Path marcher is a real-time GPU-accelerated path tracer that uses the ray marching technique in order to compute intersections with scene objects.

The application is built using C++, OpenGL and GLSL.

The path tracing rendering engine is implemented on the fragment shader. Furthermore, the system implements a simple denoising shader in order to tone down the amount of noise in the output in real time.

## Geometry supported

This rendering model supports more than simple solid geometry: it allows the path tracing of signed distance functions.

