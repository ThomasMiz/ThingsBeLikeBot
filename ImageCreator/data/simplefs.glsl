
uniform sampler2D tex;

varying vec2 fTexCoords;

void main() {
    gl_FragColor = texture2D(tex, fTexCoords);
	//gl_FragColor = vec4(fTexCoords, 1.0, 1.0);
}