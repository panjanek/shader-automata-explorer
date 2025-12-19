#version 330 core

in vec2 vUV;

uniform sampler2D uState;

out vec4 fragColor;

void main()
{
    vec4 color = texture(uState, vUV);
    fragColor = color;
}