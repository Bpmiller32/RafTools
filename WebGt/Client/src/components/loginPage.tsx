import Emitter from "../webgl/utils/eventEmitter";
import { defineComponent, onMounted, ref } from "vue";
import { pingServer } from "./apiHandler";
import volarisLogo from "../assets/volarisLogo.svg";
import { db } from "../firebase";
import { collection, doc, getDoc, getDocs } from "firebase/firestore";

export default defineComponent({
  props: {
    apiUrl: {
      type: String,
      required: true,
    },
  },
  setup(props) {
    /* ------------------------ Component state and setup ----------------------- */
    // Template refs
    const usernameRef = ref();
    const passwordRef = ref();

    const isServerOnline = ref(false);
    const isButtonEnabled = ref(true);
    const didLoginFail = ref(false);
    const isDebugEnabled = ref(false);

    onMounted(async () => {
      // Get status of BE server
      isServerOnline.value = await pingServer(props.apiUrl);

      // Check if the URL ends with #debug
      if (window.location.hash === "#debug") {
        isDebugEnabled.value = true;
      }
    });

    /* ----------------------------- Template events ---------------------------- */
    const StartAppButtonClicked = async () => {
      // Debug, TODO: remove
      Emitter.emit("startApp");
      return;

      // Firebase login check
      try {
        // Get a reference to the document
        const docRef = doc(db, "logins", usernameRef.value.value);

        // Fetch the document
        const docSnap = await getDoc(docRef);

        // Check the loginPage password against the firebase value
        const document = docSnap.data()!;

        if (passwordRef.value.value !== document.password) {
          throw new Error();
        }

        Emitter.emit("startApp");
        isButtonEnabled.value = false;
      } catch {
        didLoginFail.value = true;
        console.error("Username or password incorrect");

        // Trigger again if already failed one login
        if (didLoginFail.value === true) {
          const loginErrorLabelElement =
            document.getElementById("loginErrorLabel");
          loginErrorLabelElement?.classList.remove("animate-shake");

          setTimeout(() => {
            loginErrorLabelElement?.classList.add("animate-shake");
          }, 100);
        }
      }
    };

    const DebugButtonClicked = async () => {
      try {
        // Reference the collection
        const collectionRef = collection(db, "imageData");

        // Fetch all documents from the collection
        const querySnapshot = await getDocs(collectionRef);

        // Iterate through each document and inspect the specific property
        querySnapshot.forEach((doc) => {
          const data = doc.data();
          const timeOnImage = data["timeOnImage"];
          console.log(`Document ID: ${doc.id}, TimeOnImage [${timeOnImage}]`);
        });
      } catch {
        console.error("Error fetching documents from firestore");
      }
    };

    /* ------------------------------ Subcomponents ----------------------------- */
    const ServerStatusBadge = () => {
      return (
        <span class="mb-2 inline-flex items-center gap-x-1.5 rounded-full px-2 py-1 text-xs font-medium text-gray-100 ring-1 ring-inset ring-gray-200">
          {ServerStatusBadgeIconSelector()}
          Server status
        </span>
      );
    };

    const ServerStatusBadgeIconSelector = () => {
      if (isServerOnline.value) {
        return (
          <svg
            class="h-1.5 w-1.5 fill-green-500"
            viewBox="0 0 6 6"
            aria-hidden="true"
          >
            <circle cx="3" cy="3" r="3" />
          </svg>
        );
      } else {
        return (
          <svg
            class="h-1.5 w-1.5 fill-red-500"
            viewBox="0 0 6 6"
            aria-hidden="true"
          >
            <circle cx="3" cy="3" r="3" />
          </svg>
        );
      }
    };

    const DebugButton = () => {
      if (isDebugEnabled.value) {
        return (
          <button
            onClick={() => DebugButtonClicked()}
            class="flex items-center w-fit py-2 px-3 rounded-l-xl rounded-r-xl border border-white/50 group hover:border-indigo-600 duration-300"
          >
            <p class="text-white text-sm group-hover:text-indigo-200 duration-300">
              Debug
            </p>
          </button>
        );
      } else {
        return <div></div>;
      }
    };

    const LoginErrorLabel = () => {
      if (didLoginFail.value) {
        return (
          <div class="justify-self-end flex items-center">
            <label
              id="loginErrorLabel"
              class="text-sm text-red-500 animate-shake"
            >
              Login failed
            </label>
            ;
          </div>
        );
      }
    };

    const StartAppButton = () => {
      if (isServerOnline.value && isButtonEnabled.value) {
        return (
          <button
            type="button"
            onClick={() => StartAppButtonClicked()}
            class="rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 duration-300"
          >
            Start App
          </button>
        );
      } else {
        return (
          <button
            type="button"
            class="cursor-not-allowed rounded-md bg-gray-600 px-3 py-2 text-sm font-semibold text-white shadow-sm"
          >
            Start App
          </button>
        );
      }
    };

    /* ----------------------------- Render function ---------------------------- */
    return () => (
      <article class="w-screen h-screen flex justify-center items-center">
        <section>
          {/* App logo */}
          <div class="mb-5">
            <img src={volarisLogo} class="h-5 w-full" alt="volarisLogo" />
          </div>

          {/* Server status */}
          <div class="flex justify-end">{ServerStatusBadge()}</div>

          {/* Username and pass input fields */}
          <div class="isolate -space-y-px rounded-md shadow-sm">
            <div class="relative rounded-md rounded-b-none px-3 pb-1.5 pt-2.5 ring-1 ring-inset ring-gray-300 focus-within:z-10 focus-within:ring-2 focus-within:ring-indigo-600 duration-300">
              <label for="name" class="block text-xs font-medium text-gray-100">
                Username
              </label>
              <input
                ref={usernameRef}
                type="text"
                class="block bg-[#211d20] w-full border-0 p-0 text-gray-100 placeholder:text-gray-400 focus:ring-0"
                placeholder="billym"
              />
            </div>
            <div class="relative rounded-md rounded-t-none px-3 pb-1.5 pt-2.5 ring-1 ring-inset ring-gray-300 focus-within:z-10 focus-within:ring-2 focus-within:ring-indigo-600 duration-300">
              <label
                for="job-title"
                class="block text-xs font-medium text-gray-100"
              >
                Password
              </label>
              <input
                ref={passwordRef}
                type="password"
                class="block bg-[#211d20] w-full border-0 p-0 text-gray-100 placeholder:text-gray-400 focus:ring-0"
                placeholder="**********"
              />
            </div>
          </div>

          {/* Start app button and optional login failed */}
          <div class="grid grid-cols-3 justify-between mt-2">
            {DebugButton()}
            {StartAppButton()}
            {LoginErrorLabel()}
          </div>
        </section>
      </article>
    );
  },
});
