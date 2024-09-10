/* -------------------------------------------------------------------------- */
/*   Handler for creating and joining clipping boxes, cropping to image box   */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "../experience";
import Camera from "../camera";
import Input from "../utils/input";
import Sizes from "../utils/sizes";
import { CSG } from "three-csg-ts";
import World from "./world";

export default class ClipBoxHandler {
  private experience: Experience;
  private scene: THREE.Scene;
  private camera: Camera;
  private sizes: Sizes;
  private input: Input;
  private world: World;

  private hasMovedMouseOnce: boolean;
  private worldStartMousePosition: THREE.Vector3;
  private worldEndMousePosition: THREE.Vector3;
  private activeMesh: THREE.Mesh | null;
  private clippingBoxes: THREE.Mesh[];

  constructor() {
    // Experience fields
    this.experience = Experience.getInstance();
    this.scene = this.experience.scene;
    this.camera = this.experience.camera;
    this.sizes = this.experience.sizes;
    this.input = this.experience.input;
    this.world = this.experience.world;

    // Class fields
    this.hasMovedMouseOnce = false;
    this.worldStartMousePosition = new THREE.Vector3();
    this.worldEndMousePosition = new THREE.Vector3();
    this.activeMesh = new THREE.Mesh();
    this.clippingBoxes = [];

    // Events
    this.input.on("mouseDown", (event) => {
      this.mouseDown(event);
    });
    this.input.on("mouseMove", (event) => {
      this.mouseMove(event);
    });
    this.input.on("mouseUp", (event) => {
      this.mouseUp(event);
    });
    this.input.on("stitchBoxes", () => {
      this.stitchBoxes();
    });
    this.input.on("resetImage", () => {
      this.resetImage();
    });
  }

  /* ------------------------------ Event methods ----------------------------- */
  private mouseDown(event: MouseEvent) {
    if (event.button !== 0 || this.input.isShiftLeftPressed) {
      return;
    }

    console.log("isInteractingWithGui: ", this.input.isInteractingWithGui);

    if (this.input.isInteractingWithGui) {
      return;
    }

    this.input.isLeftClickPressed = true;

    // Convert the mouse position to world coordinates
    this.worldStartMousePosition = this.screenToSceneCoordinates(
      event.clientX,
      event.clientY
    );

    // Create a new mesh at the starting position
    const geometry = new THREE.BoxGeometry(0.2, 0.2, 0.2);
    const material = new THREE.MeshBasicMaterial({
      color: new THREE.Color(Math.random(), Math.random(), Math.random()),
      wireframe: false,
      transparent: true,
      opacity: 0.5,
    });
    this.activeMesh = new THREE.Mesh(geometry, material);
    this.activeMesh.position.set(
      this.worldStartMousePosition.x,
      this.worldStartMousePosition.y,
      5 // z-coodinate of plane to work on, ImageBox is at 0 and ClipBoxes are at 5
    );
    this.scene.add(this.activeMesh);
  }

  private mouseMove(event: MouseEvent) {
    // Handle rotating of all existing clipBoxes when in move mode
    if (this.input.isShiftLeftPressed && !this.input.isRightClickPressed) {
      // Target point and axis around which the mesh will rotate
      const targetPoint = new THREE.Vector3(0, 0, 0);
      const axis = new THREE.Vector3(0, 0, 1);

      for (let i = 0; i < this.clippingBoxes.length; i++) {
        // Translate object to the point
        this.clippingBoxes[i].position.sub(targetPoint);

        // Create rotation matrix
        this.clippingBoxes[i].position.applyAxisAngle(
          axis,
          -event.movementX * 0.005
        );

        // Translate back
        this.clippingBoxes[i].position.add(targetPoint);

        // Apply rotation to the object's orientation
        this.clippingBoxes[i].rotateOnAxis(axis, -event.movementX * 0.005);
      }

      return;
    }

    // Handle drawing of new ClipBoxes
    if (this.input.isLeftClickPressed) {
      // Gate to add behavior of box size on starting click
      if (!this.hasMovedMouseOnce) {
        this.hasMovedMouseOnce = true;

        this.activeMesh?.geometry.dispose();
        const geometry = new THREE.BoxGeometry(1, 1, 1);
        this.activeMesh!.geometry = geometry;
      }

      // Convert the mouse position to world coordinates
      this.worldEndMousePosition = this.screenToSceneCoordinates(
        event.clientX,
        event.clientY
      );

      // Calculate the width and height based on world coordinates
      const size = new THREE.Vector3(
        Math.abs(this.worldEndMousePosition.x - this.worldStartMousePosition.x),
        Math.abs(this.worldEndMousePosition.y - this.worldStartMousePosition.y),
        // Annoying to find bugfix for CSG union later, this mesh must have depth to be 3d and intersect later....
        // Math.abs(this.worldEndMousePosition.z - this.worldStartMousePosition.z)
        2 // ImageBox is depth of 1 so this fully intersects through
      );

      // Scale the mesh
      this.activeMesh?.scale.set(size.x, size.y, size.z);

      // Reposition the mesh to stay centered between start and end points
      this.activeMesh?.position.copy(
        this.worldStartMousePosition
          .clone()
          .add(this.worldEndMousePosition)
          .divideScalar(2)
      );

      return;
    }
  }

  private mouseUp(event: MouseEvent) {
    // if (this.input.isInteractingWithGui) {
    //   return;
    // }

    // Fix for isInteractingWithGui on a mouseUp
    if (this.input.dashboardGuiGlobal?.contains(event.target as HTMLElement)) {
      return;
    }

    if (event.button === 0) {
      this.input.isLeftClickPressed = false;
      this.hasMovedMouseOnce = false;

      // Add the activeMesh to the clippingBoxes array here
      this.activeMesh!.updateMatrix();
      this.clippingBoxes.push(this.activeMesh!);
    }
  }

  private async stitchBoxes() {
    if (this.clippingBoxes.length === 0) {
      return;
    }

    let combinedMesh = this.clippingBoxes[0];

    for (let i = 0; i < this.clippingBoxes.length; i++) {
      combinedMesh = CSG.union(combinedMesh, this.clippingBoxes[i]);

      this.scene.remove(this.clippingBoxes[i]);
      this.clippingBoxes[i].geometry.dispose();
    }

    // Dispose of the references in clippingBoxes, add the only existing clippingBox in case of further clips
    this.clippingBoxes.length = 0;

    // Push the combinedMesh back to the same plane as the imageBox mesh, update it's local position matrix for CSG
    combinedMesh.position.z = 0;
    combinedMesh.updateMatrix();

    // Add the new combined mesh to the scene
    const croppedMesh = CSG.intersect(
      this.world.imageBoxHandler.mesh,
      combinedMesh
    );

    // Remove the old imageBox so it doesn't overlap with the croppedMesh, set croppedMesh to imageBox
    this.scene.remove(this.world.imageBoxHandler.mesh);
    this.world.imageBoxHandler.mesh = croppedMesh;
    this.scene.add(croppedMesh);
  }

  private resetImage() {
    for (let i = 0; i < this.clippingBoxes.length; i++) {
      this.scene.remove(this.clippingBoxes[i]);
      this.clippingBoxes[i].geometry.dispose();
    }

    this.clippingBoxes.length = 0;
  }

  /* ----------------------------- Helper methods ----------------------------- */
  private screenToSceneCoordinates(
    mouseX: number,
    mouseY: number
  ): THREE.Vector3 {
    // Normalize mouse coordinates (-1 to 1)
    const ndcX = (mouseX / this.sizes.width) * 2 - 1;
    const ndcY = -(mouseY / this.sizes.height) * 2 + 1;

    // Create a vector in NDC space
    const vector = new THREE.Vector3(ndcX, ndcY, 0.5); // z=0.5 to unproject at the center of the near and far planes

    // Unproject the vector to scene coordinates
    vector.unproject(this.camera.instance);

    // Adjust the z-coordinate to match the camera's z-plane
    vector.z = 5; // Set the z-coordinate to 0 or the plane you want to work on, in this case 5 since the gtImage is at z==0

    return vector;
  }

  /* ------------------------------ Tick methods ------------------------------ */
  public destroy() {
    if (this.activeMesh) {
      this.scene.remove(this.activeMesh);
      this.activeMesh.geometry.dispose();
    }

    for (let i = 0; i < this.clippingBoxes.length; i++) {
      this.scene.remove(this.clippingBoxes[i]);
      this.clippingBoxes[i].geometry.dispose();
    }
  }
}
