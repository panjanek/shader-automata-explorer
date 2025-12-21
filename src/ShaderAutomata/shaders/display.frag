#version 330 core

in vec2 vUV;

uniform sampler2D uState;
uniform vec2 uZoomCenter;  // [0,1] texture space
uniform float uZoom;       // >1.0 = zoom in


out vec4 fragColor;

void main()
{
    //vec2 uv = (vUV - uZoomCenter) / uZoom + uZoomCenter;
    vec2 uv = (vUV - 0.5) / uZoom + uZoomCenter;
    vec4 color = texture(uState, uv);
    fragColor = color;

    if (uv.x<0 || uv.x >=1 || uv.y<0 || uv.y>=1)
        fragColor = vec4(0,0,0,1);

    //vec4 color = texture(uState, vUV);
    //fragColor = color;
}