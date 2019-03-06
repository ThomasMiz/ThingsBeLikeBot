
uniform mat4 Proj;

in vec3 vPosition;
in vec2 vTexCoords;

varying vec2 fTexCoords;

void main() {
    gl_Position = Proj * vec4(vPosition, 1.0);
	fTexCoords = vTexCoords;
}