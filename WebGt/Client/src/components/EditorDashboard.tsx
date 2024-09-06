<template></template>;

import { defineComponent } from "vue";

export default defineComponent({
  setup() {
    return () => (
      <article class="flex overflow-hidden flex-col p-6 bg-white rounded-xl shadow-xl max-w-[400px]">
        <header class="flex flex-col w-full">
          {/* <img
            loading="lazy"
            src="https://cdn.builder.io/api/v1/image/assets/TEMP/5b6c5a66f997748c4546b679d98b2b6c671ab81440f6b23a03066fc3d19e3b8e?placeholderIfAbsent=true&apiKey=8bb8a4ea1a4044d2b795743556b95ebe"
            class="object-contain w-12 aspect-square"
            alt="Blog post published icon"
          /> */}
          <div class="flex flex-col mt-5 w-full">
            <h1 class="text-lg font-semibold leading-loose text-gray-900">
              Blog post published
            </h1>
            <p class="mt-2 text-sm leading-5 text-slate-600">
              This blog post has been published. Team members will be able to
              edit this post and republish changes.
            </p>
          </div>
        </header>
        <footer class="flex gap-3 items-start mt-8 w-full text-base font-semibold whitespace-nowrap">
          <button class="flex flex-1 shrink items-start rounded-lg basis-0 text-slate-700 overflow-hidden px-5 py-2.5 w-full bg-white border border-gray-300 border-solid shadow-sm">
            Cancel
          </button>
          <button class="flex flex-1 shrink items-start text-white rounded-lg basis-0 overflow-hidden px-5 py-2.5 w-full bg-violet-500 border border-violet-500 border-solid shadow-sm">
            Confirm
          </button>
        </footer>
      </article>
    );
  },
});
