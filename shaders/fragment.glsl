#version 410 core

in vec2 v_uv;

uniform vec3      u_colour;
uniform sampler2D u_texture;
uniform int       u_useTexture;   // 0 = flat colour  1 = texture sample

out vec4 fragColour;

void main()
{
    if (u_useTexture == 1)
        fragColour = texture(u_texture, v_uv);
    else
        fragColour = vec4(u_colour, 1.0);
}
