#version 330 core

uniform sampler2D uPrevState;
uniform vec2 uTexelSize;         // (1.0/width, 1.0/height)
uniform vec2 uScreenSize;

layout(location = 0) out vec4 outState;


float inverted_bell(float x)
{
    return x*x / (x*x + 0.3);
}

float sigmoid(float x)
{
    return 0.75 + x / (2.0 * (1.0 + abs(x)));
}


void main()
{
    vec2 uv = gl_FragCoord.xy * uTexelSize;

    
    float kernel3[9] = float[](
        0.1,  -0.1,  0.3,
        0,    1,    -0.3,
        -0.5, -0.1,  0.2
    );

    float kernel5[25] = float[](
       -0.1,   0.0,   0.0,   0.1,   0.0,
        0.0,   0.3,   0.1,   0.0,   0.1,
       -0.1,   0.0,   0.6,  -0.1,   0.1,
        0.0,   0.0,   0.0,  -0.2,   0.0,
       -0.1,   0.0,   0.0,  -0.1,   0.2
    );

    float kernel5blur[25] = float[](
        0.0,   0.0,   0.0,   0.0,   0.0,
        0.0,   1.0,   1.0,   1.0,   0.0,
        0.0,   2.0,   50.0,   2.0,   0.0,
        0.0,   1.0,   2.0,   1.0,   0.0,
        0.0,   0.0,   0.0,   0.0,   0.0
    );

    float sum = 0.0;
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

            float v = texture(uPrevState, src).r;
            norm += kernel5blur[k];
            sum += v * kernel5blur[k++];
        }
    }

    float result = 0.98*(sum/norm);//inverted_bell(sum);
    if (result < 0)
      result = 0;

    if (result > 1.0)
      result=1.0;



    // Store scalar state in .r channel
    outState = vec4(result, 0.0, 0.0, 0.0);
}