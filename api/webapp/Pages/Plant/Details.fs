module webapp.Pages.Plant.Details

open domain

let render (plant: Plant) =
    $"""
         <div class="flex w-full justify-between pl-10 pt-5">
            <div class="flex">
                <div class="mr-5">
                    <img
                        alt="Image of a {plant.name}"
                        class="h-32 w-32 rounded-full object-cover"
                        src={plant.image_url}
                    />
                </div>
                <div class="content-center">
                    <h1 class="font-sans text-3xl font-bold text-lime-800">{plant.name}</h1>
                    <p class="max-w-72 pl-2 text-sm font-bold text-slate-600">
            TODO beskrivelse og tags?
                    </p>
                </div>
            </div>
          </div>
         """
