./env.ps1
npx supabase link --project-ref $env:SUPABASE_PROJECT_ID --password $env:SUPABASE_DB_PASSWORD
npx supabase db push --linked --include-all --password $env:SUPABASE_DB_PASSWORD
npx supabase functions deploy profile-bootstrap --project-ref $env:SUPABASE_PROJECT_ID
npx supabase functions deploy profile-save --project-ref $env:SUPABASE_PROJECT_ID
npx supabase functions deploy game-action --project-ref $env:SUPABASE_PROJECT_ID