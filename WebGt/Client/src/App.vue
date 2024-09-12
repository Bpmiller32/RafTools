<script setup lang="ts">
import { onMounted, ref } from "vue";
import Experience from "./webgl/experience.ts";
import EditorDashboard from "./components/editorDashboard.tsx";
import LoginPage from "./components/loginPage.tsx";
import {
  downloadImage,
  startBrowserInstance,
} from "./components/apiHandler.ts";

// App setup
const webglRef = ref<HTMLCanvasElement | null>(null);
const isAppStarted = ref(false);

const apiUrl = "https://termite-grand-moose.ngrok-free.app";
const webglExperience = Experience.getInstance();

onMounted(() => {
  webglExperience.configure(webglRef.value);

  // Only after experience is initialized, fire event so that world entities are created
  webglExperience.resources.emit("appReady");
});

// Handle app first start
const handleAppStarted = async () => {
  // Initialize browser on the server
  const serverInstanceInitialized = await startBrowserInstance(apiUrl);

  if (!serverInstanceInitialized) {
    return;
  }

  // Transition login page -> app page
  isAppStarted.value = !isAppStarted.value;

  // Pull image from current page
  const image = await downloadImage(apiUrl);

  if (!image) {
    return;
  }

  // Start image load into webgl scene as a texture, resourceLoader will trigger an event when finished loading
  webglExperience.resources.loadFromApi(image.imageBlob);

  // Set the image's name in the gui
  webglExperience.input.dashboardImageName!.innerText =
    image.imageName + ".jpg";

  // Clean up (string is very long since it is a blob of the entire image)
  URL.revokeObjectURL(image.imageBlob);
};
</script>

<template>
  <!-- Start/Login page -->
  <Transition
    leaveFromClass="opacity-100"
    leaveToClass="opacity-0"
    leaveActiveClass="duration-[500ms]"
  >
    <LoginPage
      v-if="!isAppStarted"
      @appStarted="handleAppStarted"
      id="loginPage"
      :apiUrl="apiUrl"
    />
  </Transition>

  <!-- Main app -->
  <Transition
    enterFromClass="opacity-0"
    enterToClass="opacity-100"
    enterActiveClass="duration-[2500ms]"
  >
    <main v-show="isAppStarted">
      <EditorDashboard id="gui" class="absolute" :apiUrl="apiUrl" />

      <canvas ref="webglRef"></canvas>
    </main>
  </Transition>
</template>
