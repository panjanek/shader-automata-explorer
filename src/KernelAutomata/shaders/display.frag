#version 330 core

in vec2 vUV;

uniform sampler2D uState;
uniform vec2 uZoomCenter;  // [0,1] texture space
uniform float uZoom;       // >1.0 = zoom in


out vec4 fragColor;

void main()
{

    vec2 uv = (vUV - uZoomCenter) / uZoom + uZoomCenter;
    vec4 color = texture(uState, uv);
    fragColor = color;

    //vec4 color = texture(uState, vUV);
    //fragColor = color;
}