#version 330 core

uniform sampler2D uPrevState;
uniform vec2 uTexelSize;         // (1.0/width, 1.0/height)
uniform float uKernel[25];

layout(location = 0) out vec4 outState;

void main()
{
    vec2 uv = gl_FragCoord.xy * uTexelSize;
    vec4 sum = vec4(0,0,0,0);
    float norm = 0;
    int k = 0;
    for (int j = -2; j <= 2; j++)
    {
        for (int i = -2; i <= 2; i++)
        {
            float dx = float(i);
            float dy = float(j);
            vec2 pixelOffset = vec2(dx, dy);
            vec2 offset = pixelOffset * uTexelSize;
            vec2 src = uv + offset;

            if (src.x < 0)
                src.x += 1.0;
            if (src.x > 1.0)
                src.x -= 1.0;
            if (src.y < 0)
                src.y += 1.0;
            if (src.y > 1.0)
                src.y -= 1.0;

            vec4 current = texture(uPrevState, src);
            norm += uKernel[k];
            sum += current * uKernel[k++];
        }
    }

    vec4 result = sum;
    if (result.r < 0)
      result.r = 0;
    if (result.r > 1.0)
      result.r=1.0;

    // Store scalar state in .r channel
    outState = result;
}