<script setup>
import { Disclosure, DisclosureButton, DisclosurePanel } from "@headlessui/vue";
import { MenuIcon, XIcon } from "@heroicons/vue/outline";
import { watch } from "vue";
import { useRoute } from "vue-router";
import AnimationHandler from "./AnimationHandler.vue";

const route = useRoute();

watch(
  () => route.name,
  () => {
    console.log("route changed: ", route.name);
  }
);
</script>

<template>
  <Disclosure as="nav" class="bg-white shadow-sm" v-slot="{ open }">
    <div class="flex justify-between h-16 px-4">
      <div class="flex sm:space-x-8">
        <div class="flex items-center">
          <img class="block lg:hidden h-8" src="../assets/MatwLogoSmall.png" />
          <img class="hidden lg:block h-8" src="../assets/MatwLogoLarge.png" />
        </div>
        <div class="hidden sm:flex sm:space-x-8">
          <!-- <router-link custom to="/Crawler" v-slot="{ href, isActive }">
            <AnimationHandler v-if="isActive" animation="NavFadeIn" appear>
              <a
                :href="href"
                :class="{
                  'border-indigo-600': isActive == true,
                  'border-transparent hover:border-gray-300': isActive == false,
                  'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
                }"
              >
                Crawler
              </a>
            </AnimationHandler>
            <a
              v-else
              :href="href"
              :class="{
                'border-indigo-600': isActive == true,
                'border-transparent hover:border-gray-300': isActive == false,
                'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
              }"
            >
              Crawler
            </a>
          </router-link> -->

          <!-- v-if="route.name == 'Home'" -->
          <AnimationHandler animation="NavFadeIn" appear>
            <router-link
              to="/Crawler"
              :class="{
                'border-indigo-600': route.name == 'Home',
                'border-transparent hover:border-gray-300':
                  route.name != 'Home',
                'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
              }"
              >Crawler</router-link
            >
          </AnimationHandler>

          <router-link custom to="/Builder" v-slot="{ href, isActive }">
            <AnimationHandler v-if="isActive" animation="NavFadeIn" appear>
              <a
                :href="href"
                :class="{
                  'border-indigo-600': isActive == true,
                  'border-transparent hover:border-gray-300': isActive == false,
                  'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
                }"
              >
                Builder
              </a>
            </AnimationHandler>
            <a
              v-else
              :href="href"
              :class="{
                'border-indigo-600': isActive == true,
                'border-transparent hover:border-gray-300': isActive == false,
                'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
              }"
            >
              Builder
            </a>
          </router-link>
          <router-link custom to="/Tester" v-slot="{ href, isActive }">
            <AnimationHandler v-if="isActive" animation="NavFadeIn" appear>
              <a
                :href="href"
                :class="{
                  'border-indigo-600': isActive == true,
                  'border-transparent hover:border-gray-300': isActive == false,
                  'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
                }"
              >
                Tester
              </a>
            </AnimationHandler>
            <a
              v-else
              :href="href"
              :class="{
                'border-indigo-600': isActive == true,
                'border-transparent hover:border-gray-300': isActive == false,
                'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
              }"
            >
              Tester
            </a>
          </router-link>
        </div>
      </div>
      <div class="flex items-center">
        <DisclosureButton
          class="sm:hidden bg-white p-2 rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
        >
          <MenuIcon v-if="!open" class="h-6 w-6" />
          <XIcon v-else class="h-6 w-6" />
        </DisclosureButton>
      </div>
    </div>
    <DisclosurePanel class="sm:hidden">
      <div class="pt-2 pb-3 space-y-1"></div>
    </DisclosurePanel>
  </Disclosure>
</template>
