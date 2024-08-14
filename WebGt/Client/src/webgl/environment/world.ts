/* -------------------------------------------------------------------------- */
/*         The "World" in which all resources for the webgl scene live        */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "../experience";
import ResourceLoader from "../utils/resourceLoader.ts";
import Camera from "../camera.ts";
import { CSG } from "three-csg-ts";

export default class World {
  private experience: Experience;
  private resources: ResourceLoader;
  private camera: Camera;

  // World assets

  // Raycaster
  raycaster = new THREE.Raycaster();
  mouse = new THREE.Vector2();
  plane = new THREE.Mesh();

  isDragging = false;
  startPoint = new THREE.Vector2();
  endPoint = new THREE.Vector2();
  selectionBox: any;

  // Clipping plane array
  clippingPlanes: THREE.Plane[] = [];

  constructor() {
    this.experience = Experience.getInstance();
    this.resources = this.experience.resources;
    this.camera = this.experience.camera;

    // Event listeners
    this.experience.targetElement?.addEventListener(
      "mousedown",
      this.onMouseDown
    );
    this.experience.targetElement?.addEventListener("mouseup", this.onMouseUp);
    this.experience.targetElement?.addEventListener(
      "mousemove",
      this.onMouseMove
    );

    // Resources
    this.resources?.on("ready", () => {
      // Set material
      const spriteMaterial = new THREE.SpriteMaterial({
        map: this.resources.items.test,
      });
      const boxMaterial = new THREE.MeshBasicMaterial({ color: "blue" });
      const spriteBoxMaterials = [
        new THREE.MeshBasicMaterial({ color: 0x00ff00 }), // Right face
        new THREE.MeshBasicMaterial({ color: 0xff0000 }), // Left face
        new THREE.MeshBasicMaterial({ color: 0x0000ff }), // Top face
        new THREE.MeshBasicMaterial({ color: 0xffff00 }), // Bottom face
        new THREE.MeshBasicMaterial({ map: this.resources.items.test }), // Back face with texture
        new THREE.MeshBasicMaterial({ color: 0xffffff }), // Front face
      ];

      // Set geometry
      const boxGeometry = new THREE.BoxGeometry(3, 3, 10);
      const spriteBoxGeometry = new THREE.BoxGeometry(5, 5, 5, 10, 10, 10);

      // Set "mesh"
      const sprite = new THREE.Sprite(spriteMaterial);
      const boxMesh = new THREE.Mesh(boxGeometry, boxMaterial);
      const spriteBoxMesh = new THREE.Mesh(
        spriteBoxGeometry,
        spriteBoxMaterials
      );

      boxMesh.position.x += 2;
      boxMesh.position.y += 2;

      // update matrix, updates local transform, otherwise mesh is still at 0
      boxMesh.updateMatrix();
      spriteBoxMesh.updateMatrix();

      // Add to scene
      // this.experience.scene.add(sprite);
      this.experience.scene.add(boxMesh);
      this.experience.scene.add(spriteBoxMesh);

      // const subRes = CSG.subtract(boxMesh, spriteBoxMesh);
      const subRes = CSG.subtract(spriteBoxMesh, boxMesh);
      subRes.position.x -= 7;
      subRes.material = spriteBoxMaterials;

      this.experience.scene.add(subRes);

      // Add a plane to the scene
      const geometry = new THREE.PlaneGeometry(200, 200);
      const material = new THREE.MeshBasicMaterial({
        color: 0x00ff00,
        side: THREE.DoubleSide,
      });
      this.plane = new THREE.Mesh(geometry, material);
      // this.experience.scene.add(this.plane);
    });
  }
  // Function to update mouse coordinates
  updateMouseCoords(event: MouseEvent) {
    this.mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
    this.mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;
  }

  // Mouse down event
  onMouseDown = (event: MouseEvent) => {
    this.updateMouseCoords(event);

    this.raycaster.setFromCamera(this.mouse, this.camera.instance);
    const intersects = this.raycaster.intersectObjects(
      this.experience.scene.children
    );

    if (intersects.length > 0) {
      this.isDragging = true;
      this.startPoint.set(event.clientX, event.clientY);

      if (!this.selectionBox) {
        this.selectionBox = document.createElement("div");
        this.selectionBox.style.border = "1px solid red";
        this.selectionBox.style.position = "absolute";
        document.body.appendChild(this.selectionBox);
      }
    }
  };

  // Mouse move event
  onMouseMove = (event: MouseEvent) => {
    if (this.isDragging) {
      this.endPoint.set(event.clientX, event.clientY);

      this.selectionBox.style.left =
        Math.min(this.startPoint.x, this.endPoint.x) + "px";
      this.selectionBox.style.top =
        Math.min(this.startPoint.y, this.endPoint.y) + "px";
      this.selectionBox.style.width =
        Math.abs(this.startPoint.x - this.endPoint.x) + "px";
      this.selectionBox.style.height =
        Math.abs(this.startPoint.y - this.endPoint.y) + "px";
    }
  };

  // Mouse up event
  onMouseUp = (event: MouseEvent) => {
    if (this.isDragging) {
      this.isDragging = false;

      // Get the box coordinates
      const box = {
        x: Math.min(this.startPoint.x, this.endPoint.x),
        y: Math.min(this.startPoint.y, this.endPoint.y),
        width: Math.abs(this.startPoint.x - this.endPoint.x),
        height: Math.abs(this.startPoint.y - this.endPoint.y),
      };

      // Convert box coordinates to world coordinates
      const topLeft = new THREE.Vector2(
        (box.x / window.innerWidth) * 2 - 1,
        -(box.y / window.innerHeight) * 2 + 1
      );
      const bottomRight = new THREE.Vector2(
        ((box.x + box.width) / window.innerWidth) * 2 - 1,
        -((box.y + box.height) / window.innerHeight) * 2 + 1
      );

      // Set raycaster to find top left and bottom right intersections
      this.raycaster.setFromCamera(topLeft, this.camera.instance);
      const topLeftIntersect = this.raycaster.intersectObject(this.plane)[0]
        .point;

      this.raycaster.setFromCamera(bottomRight, this.camera.instance);
      const bottomRightIntersect = this.raycaster.intersectObject(this.plane)[0]
        .point;

      // Define clipping planes
      this.clippingPlanes = [
        new THREE.Plane(new THREE.Vector3(1, 0, 0), -topLeftIntersect.x),
        new THREE.Plane(new THREE.Vector3(-1, 0, 0), bottomRightIntersect.x),
        new THREE.Plane(new THREE.Vector3(0, 1, 0), -topLeftIntersect.y),
        new THREE.Plane(new THREE.Vector3(0, -1, 0), bottomRightIntersect.y),
      ];

      // Type assertion to ensure clippingPlanes is accepted
      (this.plane.material as THREE.MeshBasicMaterial).clippingPlanes =
        this.clippingPlanes;
      (this.plane.material as THREE.MeshBasicMaterial).clipIntersection = true;

      // Remove the selection box
      document.body.removeChild(this.selectionBox);
      this.selectionBox = null;
    }
  };

  public update() {}

  public destroy() {}
}
