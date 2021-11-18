#version 450

out vec4 outputColor;
in vec2 TexCoord;
in vec3 Normal;
flat in int TexId;

uniform sampler2DArray texture0;

void main()
{
    outputColor = texture(texture0, vec3(TexCoord, TexId));
}