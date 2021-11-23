#version 450
in vec3 aPos;
in vec2 aTexCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec2 TexCoord;
out vec3 Pos;

void main()
{
    gl_Position =  projection * model * vec4(aPos, 1.0);

    TexCoord = aTexCoord;
    Pos = aPos;
}