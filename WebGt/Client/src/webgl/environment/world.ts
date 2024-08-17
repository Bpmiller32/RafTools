/* -------------------------------------------------------------------------- */
/*         The "World" in which all resources for the webgl scene live        */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "../experience";
import ResourceLoader from "../utils/resourceLoader.ts";
import Camera from "../camera.ts";
import { CSG } from "three-csg-ts";
import Input from "../utils/input.ts";
import CroppingBox from "./croppingBox.ts";

export default class World {
  private experience: Experience;
  private resources: ResourceLoader;
  private camera: Camera;
  private input: Input;

  constructor() {
    this.experience = Experience.getInstance();
    this.resources = this.experience.resources;
    this.camera = this.experience.camera;
    this.input = this.experience.input;

    // Resources events
    this.resources.on("ready", () => {
      // Set material
      const boxMaterial = new THREE.MeshBasicMaterial({ color: "blue" });
      const spriteBoxMaterials = [
        new THREE.MeshBasicMaterial({ color: 0x00ff00 }), // Right face
        new THREE.MeshBasicMaterial({ color: 0xff0000 }), // Left face
        new THREE.MeshBasicMaterial({ color: 0x0000ff }), // Top face
        new THREE.MeshBasicMaterial({ color: 0xffff00 }), // Bottom face
        new THREE.MeshBasicMaterial({ map: this.resources.items.test }), // Front face with texture
        new THREE.MeshBasicMaterial({ color: 0xffffff }), // Back face
      ];

      // Set geometry
      const boxGeometry = new THREE.BoxGeometry(3, 3, 10);
      const spriteBoxGeometry = new THREE.BoxGeometry(5, 5, 5, 10, 10, 10);

      // Set "mesh"
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
      this.experience.scene.add(boxMesh);
      this.experience.scene.add(spriteBoxMesh);

      // const subRes = CSG.subtract(boxMesh, spriteBoxMesh);
      const subRes = CSG.subtract(spriteBoxMesh, boxMesh);
      subRes.position.x -= 7;
      subRes.material = spriteBoxMaterials;

      this.experience.scene.add(subRes);
    });

    // Input events
    this.input.on("newCroppingBox", () => {
      new CroppingBox(this.input.clickStartPoint, this.input.clickEndPoint);
    });
  }

  public update() {
    this.camera.update();
  }

  public destroy() {}
}
