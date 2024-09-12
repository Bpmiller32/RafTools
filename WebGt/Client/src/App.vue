<script setup lang="ts">
import { onMounted, ref } from "vue";
import Experience from "./webgl/experience.ts";
import EditorDashboard from "./components/editorDashboard.tsx";
import LoginPage from "./components/loginPage.tsx";

// Component setup
const webglRef = ref<HTMLCanvasElement | null>(null);
const isAppStarted = ref(false);

onMounted(() => {
  const webglExperience = Experience.getInstance();
  webglExperience.configure(webglRef.value);
});

// Handle events
const handleAppStarted = () => {
  isAppStarted.value = !isAppStarted.value;
  // TODO: start image download
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
      apiUrl="https://termite-grand-moose.ngrok-free.app"
    />
  </Transition>

  <!-- Main app -->
  <Transition
    enterFromClass="opacity-0"
    enterToClass="opacity-100"
    enterActiveClass="duration-[2500ms]"
  >
    <main v-show="isAppStarted">
      <EditorDashboard
        id="gui"
        class="absolute"
        apiUrl="https://termite-grand-moose.ngrok-free.app"
      />

      <canvas ref="webglRef"></canvas>
    </main>
  </Transition>
</template>
