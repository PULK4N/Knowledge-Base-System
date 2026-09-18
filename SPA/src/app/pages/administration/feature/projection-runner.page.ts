import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import {
  Observable,
  Subject,
  catchError,
  exhaustMap,
  map,
  of,
  startWith,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import {
  ProjectionGroup,
  ProjectionRunScope,
  RunProjectionRequest,
} from '../data-access/projection-administration.models';
import { ProjectionAdministrationService } from '../data-access/projection-administration.service';

const aggregateIdPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const nonWhitespaceValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null =>
  String(control.value ?? '').trim() ? null : { required: true };

const projectionTargetValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const scope = control.get('scope')?.value as
    | ProjectionRunScope
    | undefined;
  const aggregateId = String(
    control.get('aggregateId')?.value ?? '',
  ).trim();
  const stateMachineId = String(
    control.get('stateMachineId')?.value ?? '',
  ).trim();

  if (scope === 'aggregate') {
    if (!aggregateId) return { targetRequired: true };
    return aggregateIdPattern.test(aggregateId)
      ? null
      : { aggregateIdInvalid: true };
  }

  return stateMachineId ? null : { targetRequired: true };
};

type RunState =
  | { readonly status: 'idle' }
  | {
      readonly status: 'running';
      readonly request: RunProjectionRequest;
    }
  | {
      readonly status: 'success';
      readonly request: RunProjectionRequest;
      readonly processedAggregateCount: number;
    }
  | {
      readonly status: 'error';
      readonly request: RunProjectionRequest;
      readonly message: string;
    };

/**
 * Direct single-projector execution form rendered inside the Projections
 * tab. The host page owns the projection list and passes it in so the
 * runner can offer projector and state-machine suggestions without a
 * second request.
 */
@Component({
  selector: 'app-projection-runner-page',
  imports: [AsyncPipe, ReactiveFormsModule],
  templateUrl: './projection-runner.page.html',
  styleUrl: './projection-runner.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectionRunnerPage {
  private readonly administration = inject(ProjectionAdministrationService);
  private readonly runRequests = new Subject<RunProjectionRequest>();

  readonly projections =
    input.required<LoadState<readonly ProjectionGroup[]>>();

  protected readonly form = new FormGroup(
    {
      projectionName: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, nonWhitespaceValidator],
      }),
      scope: new FormControl<ProjectionRunScope>('aggregate', {
        nonNullable: true,
      }),
      aggregateId: new FormControl('', { nonNullable: true }),
      stateMachineId: new FormControl('', { nonNullable: true }),
    },
    { validators: projectionTargetValidator },
  );

  protected readonly run$: Observable<RunState> = this.runRequests.pipe(
    exhaustMap(request =>
      this.administration.run(request).pipe(
        map(
          result =>
            ({
              status: 'success',
              request,
              processedAggregateCount: result.processedAggregateCount,
            }) as const,
        ),
        startWith({ status: 'running', request } as const),
        catchError(error =>
          of({
            status: 'error',
            request,
            message: toUserMessage(error),
          } as const),
        ),
      ),
    ),
    startWith({ status: 'idle' } as const),
  );

  protected run(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const projectionName = value.projectionName.trim();
    const request: RunProjectionRequest =
      value.scope === 'aggregate'
        ? {
            projectionName,
            aggregateId: value.aggregateId.trim(),
          }
        : {
            projectionName,
            stateMachineId: value.stateMachineId.trim(),
          };

    this.runRequests.next(request);
  }
}
