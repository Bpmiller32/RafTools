<script setup>
import { StatusOfflineIcon } from "@heroicons/vue/outline";
</script>

<template>
  <main class="py-16 select-none">
    <div class="flex justify-center">
      <img class="h-12" src="../assets/MatwLogoSmall.png" />
    </div>
    <div class="py-16">
      <div class="text-center">
        <div class="flex justify-center items-center space-x-5">
          <h1 class="text-4xl font-extrabold text-gray-900 tracking-tight">
            Disconnected from back end
          </h1>
          <StatusOfflineIcon
            class="mt-1 h-10 w-10 min-h-[2.5rem] min-w-[2.5rem]"
          />
        </div>

        <p class="mt-2 text-base text-gray-500">
          There is a problem with the connection to the backend service.
        </p>
        <p class="text-base text-gray-500">Check the logs for more details.</p>
      </div>
    </div>
  </main>
</template>
