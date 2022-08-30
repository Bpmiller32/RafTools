<script setup>
import { Disclosure, DisclosureButton, DisclosurePanel } from "@headlessui/vue";
import { MenuIcon, XIcon } from "@heroicons/vue/outline";
import { ref } from "vue";
import { useRoute } from "vue-router";
import AnimationHandler from "./AnimationHandler.vue";

const route = ref(useRoute());

const disclosureState = ref({
  isOpen: null,
  SetState: (open) => {
    if (open) {
      disclosureState.value.isOpen = false;
    } else {
      disclosureState.value.isOpen = true;
    }
  },
});
</script>

<template>
  <Disclosure as="nav" class="bg-white shadow-sm select-none" v-slot="{ open }">
    <div class="flex justify-between h-16 px-4">
      <div class="flex sm:space-x-8">
        <div class="flex items-center">
          <img class="block lg:hidden h-8" src="../assets/MatwLogoSmall.png" />
          <img class="hidden lg:block h-8" src="../assets/MatwLogoLarge.png" />
        </div>
        <div class="hidden sm:flex sm:space-x-8">
          <AnimationHandler
            v-if="route.name == 'Home'"
            animation="NavFadeIn"
            appear
          >
            <router-link
              to="/Crawler"
              class="border-indigo-600 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2"
              >Download</router-link
            >
          </AnimationHandler>
          <router-link
            v-else
            to="/Crawler"
            class="transition-border-color duration-500 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2 border-transparent hover:border-gray-300"
            >Download</router-link
          >

          <AnimationHandler
            v-if="route.name == 'Builder'"
            animation="NavFadeIn"
            appear
          >
            <router-link
              to="/Builder"
              class="border-indigo-600 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2"
              >Build</router-link
            >
          </AnimationHandler>
          <router-link
            v-else
            to="/Builder"
            class="transition-border-color duration-500 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2 border-transparent hover:border-gray-300"
            >Build</router-link
          >

          <AnimationHandler
            v-if="route.name == 'Tester'"
            animation="NavFadeIn"
            appear
          >
            <router-link
              to="/Tester"
              class="border-indigo-600 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2"
              >Test</router-link
            >
          </AnimationHandler>
          <router-link
            v-else
            to="/Tester"
            class="transition-border-color duration-500 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2 border-transparent hover:border-gray-300"
            >Test</router-link
          >

          <AnimationHandler
            v-if="route.name == 'Publish'"
            animation="NavFadeIn"
            appear
          >
            <router-link
              to="/Publish"
              class="border-indigo-600 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2"
              >Publish</router-link
            >
          </AnimationHandler>
          <router-link
            v-else
            to="/Publish"
            class="transition-border-color duration-500 flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2 border-transparent hover:border-gray-300"
            >Publish</router-link
          >
        </div>
      </div>
      <div class="flex items-center">
        <DisclosureButton
          @click="disclosureState.SetState(open)"
          class="sm:hidden transition-background-color transition-color duration-500 bg-white p-2 rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
        >
          <MenuIcon v-if="!open" class="h-6 w-6" />
          <XIcon v-else class="h-6 w-6" />
        </DisclosureButton>
      </div>
    </div>
    <AnimationHandler animation="SlideDown" args="nav">
      <DisclosurePanel class="sm:hidden overflow-hidden h-0">
        <div class="pb-3 space-y-1 border-t-2">
          <router-link
            to="/Crawler"
            :class="{
              'border-indigo-600 bg-indigo-50': route.name == 'Home',
              'transition-background-color duration-500 hover:bg-gray-100':
                route.name != 'Home',
              'block border-l-4 border-gray-300 p-3 text-base font-medium': true,
            }"
            >Download
          </router-link>
          <router-link
            to="/Builder"
            :class="{
              'border-indigo-600 bg-indigo-50': route.name == 'Builder',
              'transition-background-color duration-500 hover:bg-gray-100':
                route.name != 'Builder',
              'block border-l-4 border-gray-300 p-3 text-base font-medium': true,
            }"
            >Build
          </router-link>
          <router-link
            to="/Tester"
            :class="{
              'border-indigo-600 bg-indigo-50': route.name == 'Tester',
              'transition-background-color duration-500 hover:bg-gray-100':
                route.name != 'Tester',
              'block border-l-4 border-gray-300 p-3 text-base font-medium': true,
            }"
            >Test
          </router-link>
          <router-link
            to="/Publish"
            :class="{
              'border-indigo-600 bg-indigo-50': route.name == 'Publish',
              'transition-background-color duration-500 hover:bg-gray-100':
                route.name != 'Publish',
              'block border-l-4 border-gray-300 p-3 text-base font-medium': true,
            }"
            >Publish
          </router-link>
        </div>
      </DisclosurePanel>
    </AnimationHandler>
  </Disclosure>
</template>
