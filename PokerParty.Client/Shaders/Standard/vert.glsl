#version 450
in vec3 aPos;
in vec2 aTexCoord;
in vec3 aNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec2 TexCoord;
out vec3 Normal;

void main()
{
    gl_Position = projection * view * model * vec4(aPos, 1.0);

    TexCoord = aTexCoord;
    Normal = aNormal;
}