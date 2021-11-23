#version 450

in vec2 TexCoord;
in vec3 Pos;

uniform sampler2D texture0;
uniform vec3 size;
uniform vec3 texSize;
uniform int border;

out vec4 outputColor;

void main()
{
    vec2 tc = TexCoord;

    if(border != 0){
        int radius = border;

    //    // Top-left UV
    //    tc.x = Pos.x / texSize.x;
    //    tc.y = Pos.y / texSize.y;
    //
    //    // Bottom-right UV
    //    tc.y = (size.y + Pos.y) / texSize.y;
    //    tc.x = (size.x - Pos.x) / texSize.x;
    
    
        tc.x = 0.5;
        tc.y = 0.5;

        //Top
        if(Pos.y > -border){
            tc.x = Pos.x;
            tc.y = Pos.y / texSize.y;
        }

        //Bottom
        if(Pos.y < -size.y + border){
            tc.y = (size.y + Pos.y) / texSize.y;
            tc.x = (size.x - Pos.x);
        }

        //Left
        if(Pos.x < border){
            tc.x = Pos.x / texSize.x;
            tc.y = Pos.y;
        }

        // Right
        if(Pos.x > size.x - border){
            tc.y = (size.y + Pos.y);
            tc.x = (size.x - Pos.x) / texSize.x;
        }



        //Top-left
        if(Pos.x < border && Pos.y > -border){
            tc.x = Pos.x / texSize.x;
            tc.y = Pos.y / texSize.y;
        }

        //Top-right
        if(Pos.x > size.x - border && Pos.y > -border){
            tc.x = (size.x - Pos.x) / texSize.x;
            tc.y = Pos.y / texSize.y;
        }

        //Bottom-left
        if(Pos.x < border && Pos.y < -size.y + border){
            tc.x = Pos.x / texSize.x;
            tc.y = (size.y + Pos.y) / texSize.y;
        }

        // Bottom-right
        if(Pos.x > size.x - border && Pos.y < -size.y + border){
            tc.x = (size.x - Pos.x) / texSize.x;
            tc.y = (size.y + Pos.y) / texSize.y;
        }



//        // Top
//        if(Pos.x < border || Pos.y > -border) {
//            tc.x = Pos.x / texSize.x;
//            tc.y = Pos.y / texSize.y;
//        } else
//
//        // Right
//        if(Pos.x > size.x - border || Pos.y < -size.y + border){
//            tc.y = (size.y + Pos.y) / texSize.y;
//            tc.x = (size.x - Pos.x) / texSize.x;
//        }   else {
//            tc.x = 0.5;
//            tc.y = 0.5;
//        }
    }

   outputColor = texture(texture0, tc);
}