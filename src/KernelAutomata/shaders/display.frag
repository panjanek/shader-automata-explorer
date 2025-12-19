#version 330 core

in vec2 vUV;

uniform sampler2D uState;

out vec4 fragColor;

void main()
{
    float v = texture(uState, vUV).r;
    fragColor = vec4(v, v, v, 1.0);
}