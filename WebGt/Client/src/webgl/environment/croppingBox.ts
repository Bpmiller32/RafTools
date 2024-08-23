import * as THREE from "three";
import Experience from "../experience";
import Camera from "../camera";
import Input from "../utils/input";

export default class CroppingBox {
  private experience: Experience;
  private scene: THREE.Scene;
  private camera: Camera;
  private input: Input;

  private geometry!: THREE.BoxGeometry;
  private material!: THREE.MeshBasicMaterial;
  public mesh!: THREE.Mesh;

  private raycaster: THREE.Raycaster;

  constructor(startPoint: THREE.Vector3, endPoint: THREE.Vector3) {
    this.experience = Experience.getInstance();
    this.scene = this.experience.scene;
    this.camera = this.experience.camera;
    this.input = this.experience.input;

    this.raycaster = new THREE.Raycaster();

    this.setGeometry(startPoint, endPoint);
    this.setMaterial();
    this.setMesh(startPoint, endPoint);
  }

  private setGeometry(startPoint: THREE.Vector3, endPoint: THREE.Vector3) {
    // Initial size, will be updated
    this.geometry = new THREE.BoxGeometry(1, 1, 1);
  }

  private setMaterial() {
    const color = new THREE.Color(Math.random(), Math.random(), Math.random());
    this.material = new THREE.MeshBasicMaterial({
      color: color,
      wireframe: true,
      transparent: true,
      opacity: 0.5,
    });
  }

  private setMesh(startPoint: THREE.Vector3, endPoint: THREE.Vector3) {
    this.mesh = new THREE.Mesh(this.geometry, this.material);
    this.mesh.position.copy(startPoint);
    this.mesh.name = "croppingBox";
    this.scene.add(this.mesh);
  }

  public castRay() {
    const direction = new THREE.Vector3(0, 0, -1)
      .subVectors(this.mesh.position, this.camera.instance.position)
      .normalize();
    this.raycaster.set(this.camera.instance.position, direction);

    // Find intersecting objects in the scene
    const intersects = this.raycaster.intersectObjects(
      this.scene.children,
      true
    );

    if (intersects.length > 0) {
      let intersection: THREE.Intersection | undefined;

      for (let i = 0; i < intersects.length; i++) {
        if (intersects[i].object.name === "gtImage") {
          intersection = intersects[i];
          break;
        }
      }

      if (intersection !== undefined) {
        // Calculate the scale factor based on the distance from the camera
        const distanceToBox = this.camera.instance.position.distanceTo(
          this.mesh.position
        );
        const distanceToHitPoint = this.camera.instance.position.distanceTo(
          intersection.point
        );
        const scaleFactor = distanceToHitPoint / distanceToBox;

        // Create a new box at the intersection point
        // Scale the original box's dimensions
        const geometry = new THREE.BoxGeometry(
          this.mesh.scale.x * scaleFactor,
          this.mesh.scale.y * scaleFactor,
          1
        );

        this.material.wireframe = false;
        this.material.opacity = 0.65;

        const castBox = new THREE.Mesh(geometry, this.material);
        castBox.position.copy(intersection.point);
        castBox.position.z -= 0.49;
        this.scene.add(castBox);

        this.scene.remove(this.mesh);
        this.mesh = castBox;
        return;
      }
    }

    // Removing original mesh
    console.log("removed mesh?");
    this.scene.remove(this.mesh);
    // TODO: remove geometry and material as well
  }

  public updateSize() {
    const size = new THREE.Vector3(
      Math.abs(this.input.clickEndPoint.x - this.input.clickStartPoint.x),
      Math.abs(this.input.clickEndPoint.y - this.input.clickStartPoint.y),
      Math.abs(this.input.clickEndPoint.z - this.input.clickStartPoint.z)
    );

    this.mesh.scale.set(size.x, size.y, size.z);

    // Reposition the dragBox to stay centered between start and end points
    this.mesh.position.copy(
      this.input.clickStartPoint
        .clone()
        .add(this.input.clickEndPoint)
        .divideScalar(2)
    );
  }

  public destroy() {
    this.geometry.dispose();
    this.material.dispose();
    this.scene.remove(this.mesh);
  }
}
