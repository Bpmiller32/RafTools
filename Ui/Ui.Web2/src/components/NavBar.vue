<script setup>
import { Disclosure, DisclosureButton, DisclosurePanel } from "@headlessui/vue";
import { MenuIcon, XIcon } from "@heroicons/vue/outline";
import AnimationHandler from "./AnimationHandler.vue";
</script>

<template>
  <Disclosure as="nav" class="bg-white shadow-sm" v-slot="{ open }">
    <div class="flex justify-between h-16 px-4">
      <div class="flex sm:space-x-8">
        <div class="flex items-center">
          <img
            class="block lg:hidden h-8"
            src="https://tailwindui.com/img/logos/workflow-mark-indigo-600.svg"
          />
          <img
            class="hidden lg:block h-8"
            src="https://tailwindui.com/img/logos/workflow-logo-indigo-600-mark-gray-800-text.svg"
          />
        </div>
        <div class="hidden sm:flex sm:space-x-8">
          <router-link custom to="/Crawler" v-slot="{ href, isActive }">
            <a
              :href="href"
              :class="{
                'border-blue-500': isActive == true,
                'border-transparent hover:border-gray-300': isActive == false,
                'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
              }"
            >
              Crawler
            </a>
          </router-link>
          <router-link custom to="/Builder" v-slot="{ href, isActive }">
            <a
              :href="href"
              :class="{
                'border-blue-500': isActive == true,
                'border-transparent hover:border-gray-300': isActive == false,
                'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2': true,
              }"
            >
              Builder
            </a>
          </router-link>
          <router-link custom to="/Tester" v-slot="{ href, isActive }">
            <a
              :href="href"
              :class="{
                'border-blue-500': isActive == true,
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
