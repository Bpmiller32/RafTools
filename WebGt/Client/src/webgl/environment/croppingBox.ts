import * as THREE from "three";
import Experience from "../experience";
import Camera from "../camera";

export default class CroppingBox {
  private experience: Experience;
  private scene: THREE.Scene;
  private camera: Camera;

  private geometry!: THREE.BoxGeometry;
  private material!: THREE.MeshBasicMaterial;
  public mesh!: THREE.Mesh;

  private raycaster: THREE.Raycaster;

  constructor(startPoint: THREE.Vector3, endPoint: THREE.Vector3) {
    this.experience = Experience.getInstance();
    this.scene = this.experience.scene;
    this.camera = this.experience.camera;
    this.raycaster = new THREE.Raycaster();

    this.setGeometry(startPoint, endPoint);
    this.setMaterial();
    this.setMesh(startPoint, endPoint);

    this.castRay();
  }

  private setGeometry(startPoint: THREE.Vector3, endPoint: THREE.Vector3) {
    this.geometry = new THREE.BoxGeometry(
      Math.abs(endPoint.x - startPoint.x),
      Math.abs(endPoint.y - startPoint.y),
      Math.abs(endPoint.z - startPoint.z)
    );
  }

  private setMaterial() {
    const color = new THREE.Color(Math.random(), Math.random(), Math.random());
    this.material = new THREE.MeshBasicMaterial({ color });
  }

  private setMesh(startPoint: THREE.Vector3, endPoint: THREE.Vector3) {
    this.mesh = new THREE.Mesh(this.geometry, this.material);
    this.mesh.position.copy(startPoint.clone().add(endPoint).divideScalar(2));
    this.mesh.geometry.scale(1, 1, 10);
    this.scene.add(this.mesh);
  }

  private castRay() {
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
      // Get the closest intersection point
      const intersection = intersects[0];

      // Calculate the scale factor based on the distance from the camera
      const distanceToBox = this.camera.instance.position.distanceTo(
        this.mesh.position
      );
      const distanceToHitPoint = this.camera.instance.position.distanceTo(
        intersection.point
      );
      const scaleFactor = distanceToHitPoint / distanceToBox;

      // Create a new box at the intersection point
      const originalGeometry = this.geometry.parameters;
      // Scale the original box's dimensions
      const geometry = new THREE.BoxGeometry(
        originalGeometry.width * scaleFactor,
        originalGeometry.height * scaleFactor,
        originalGeometry.depth * scaleFactor
      );

      const newBox = new THREE.Mesh(geometry, this.material);
      newBox.position.copy(intersection.point);
      newBox.position.z += 0.001;
      this.scene.add(newBox);
    }
    this.scene.remove(this.mesh);
    // TODO: remove geometry and material as well
  }

  public update() {}

  public destroy() {
    this.geometry?.dispose();
    this.material?.dispose();
  }
}
