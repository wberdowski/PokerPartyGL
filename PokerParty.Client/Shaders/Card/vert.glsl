#version 450
in vec3 aPos;
in vec2 aTexCoord;
in vec3 aNormal;

in mat4 aMat;
in float aTexId;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec2 TexCoord;
out vec3 Normal;
flat out int TexId;

void main()
{
    gl_Position = projection * view * model * aMat * vec4(aPos, 1.0);

    TexCoord = aTexCoord;
    Normal = aNormal;
    TexId = int(aTexId);
}